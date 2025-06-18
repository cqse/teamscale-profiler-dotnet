#pragma once
#include <corprof.h>
#include <limits.h>
#include <vector>
#include <memory>

namespace Profiler {
	/// <summary>
	/// Set that can only contain functionIDs, which are just unsigned ints.
	/// This is based on an array and is a lot faster than the default set implementation of the standard library for this use case.
	/// </summary>
	class FunctionIdSet final
	{
	private:
		const static unsigned int DEFAULT_SIZE = 131'072;
		const static unsigned int NUM_XOR_VALUES = 10U;

		static const FunctionID rotationMask = (-1) & (CHAR_BIT * sizeof(FunctionID) - 1);

		unsigned int currentSize = DEFAULT_SIZE;
		unsigned int numElements = 0;
		unsigned int maxElements = DEFAULT_SIZE / 2;
		unsigned int moduloMask = currentSize - 1;
		std::unique_ptr<FunctionID[]> set{ new FunctionID[DEFAULT_SIZE] {0} };


		/// <summary>
		/// Updates the size of the array and reinserts the element into the new array.
		/// </summary>
		void increase_size() {
			maxElements = currentSize;
			currentSize *= 2;
			moduloMask = currentSize - 1;
			numElements = 0;
			std::unique_ptr<FunctionID[]> old_set = move(set);
			set = std::make_unique<FunctionID[]>(currentSize);
			std::fill_n(set.get(), currentSize, 0);
			for (unsigned int i = 0; i < maxElements; i++) {
				if (old_set[i] != 0) {
					insert(old_set[i]);
				}
			}
		}

		/// <summary>
		/// Circular Shift/Rotate integer by one to the right.
		/// </summary>
		static inline FunctionID rotr(FunctionID f) {
			return (f >> 1) | (f << rotationMask);
		}

		inline bool try_insert(unsigned int position, const FunctionID f) {
			if (set[position] == f) {
				return true;
			}
			if (set[position] == 0) {
				numElements++;
				if (numElements > maxElements) {
					increase_size();
					insert(f);
					return true;
				}

				set[position] = f;
				return true;
			}
			return false;
		}

		inline unsigned int nextPosition(byte& moveCounter, FunctionID& currentValue) {
			if (moveCounter == 3) {
				moveCounter = 0;
				currentValue = hash(currentValue);
			}
			else {
				currentValue++;
				moveCounter++;
			}
			return currentValue & moduloMask;
		}


		/// <summary>
		/// Hash function for integers/longs as found on https://github.com/skeeto/hash-prospector
		/// Also relevant: discussion here https://www.reddit.com/r/RNG/comments/jqnq20/the_wang_and_jenkins_integer_hash_functions_just/
		/// </summary>
		inline FunctionID hash(FunctionID f) {
#ifdef _WIN64
			f ^= f >> 30;
			f *= 0xbf58476d1ce4e5b9;
			f ^= f >> 27;
			f *= 0x94d049bb133111eb;
			f ^= f >> 31;
			return f;
#else
			f ^= f >> 16;
			f *= 0x21f0aaadU;
			f ^= f >> 15;
			f *= 0x735a2d97U;
			f ^= f >> 15;
			return f;
#endif
		}


	public:

		/// <summary>
		/// Empties the set and sets back the size of the underlying array.
		/// </summary>
		void clear() {
			currentSize = DEFAULT_SIZE;
			maxElements = DEFAULT_SIZE / 2;
			numElements = 0;
			set.reset(new FunctionID[DEFAULT_SIZE]{ });
		}

		/// <summary>
		/// Current size of the set.
		/// </summary>
		unsigned int size() {
			return currentSize;
		}

		/// <summary>
		/// Get the element at index i of the underlying array.
		/// </summary>
		FunctionID at(unsigned int i) {
			return set[i];
		}

		/// <summary>
		/// True if the set contains FunctionID f, false otherwise.
		/// </summary>
		bool contains(FunctionID f) {
			// Check the number modulo the size of the set first
			unsigned int position = f & moduloMask;
			if (set[position] == 0) {
				return false;
			}
			if (set[position] == f) {
				return true;
			}

			// Apply hash function until we found the value or an empty spot
			FunctionID currentValue = f;
			byte moveCounter = 0;
			while (set[position] != f) {
				position = nextPosition(moveCounter, currentValue);
				if (set[position] == 0) {
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Inserts FunctionID f into the set.
		/// </summary>
		void insert(FunctionID f) {
			// Try insertion at the number modulo the size of the set first
			unsigned int position = f & moduloMask;
			if (try_insert(position, f))
			{
				return;
			}

			// Then rotate bits and xor to try and find a new position
			FunctionID currentValue = f;
			byte moveCounter = 0;
			while (!try_insert(position, f)) {
				position = nextPosition(moveCounter, currentValue);
			}
		}
	};
}

