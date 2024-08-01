#pragma once
#include <corprof.h>
#include <limits.h>

/// <summary>
/// Set that can only contain functionIDs, which are just unsigned ints.
/// This is based on an array and is a lot faster than the default set implementation of the standard library for this use case.
/// </summary>
class function_id_set final
{
private:
	const static unsigned int DEFAULT_SIZE = 131'072;
	const static unsigned int NUM_XOR_VALUES = 10U;

	static const FunctionID rotation_mask = (-1) & (CHAR_BIT * sizeof(FunctionID) - 1);

	unsigned int current_size = DEFAULT_SIZE;
	unsigned int num_elements = 0;
	unsigned int max_elements = DEFAULT_SIZE / 2;
	unsigned int modulo_mask = current_size - 1;
	FunctionID* set = new FunctionID[DEFAULT_SIZE]{ 0 };

	// Predetermined values for xor operation to place in different spots
#ifdef _WIN64
	FunctionID xor_values[NUM_XOR_VALUES] = { 14653048717414601650ULL, 10827059576848633699ULL, 18275351206681430557ULL, 15388157215360276699ULL, 5625538261940547542ULL, 17184451615065543950ULL, 12483508915876842786ULL, 7009288139810362683ULL, 8675975670288616007ULL, 11886397353506492918ULL };
#else
	FunctionID xor_values[NUM_XOR_VALUES] = { 987205454U, 278240680U, 4130902882U, 445831414U, 3235577889U, 1789497761U, 205336377U, 2382455698U, 1977849072U, 966072234U };
#endif

	/// <summary>
	/// Updates the size of the array and reinserts the element into the new array.
	/// </summary>
	void increase_size() {
		max_elements = current_size;
		current_size *= 2;
		modulo_mask = current_size - 1;
		num_elements = 0;
		FunctionID* old_set = set;
		set = new FunctionID[current_size]{ 0 };
		for (unsigned int i = 0; i < max_elements; i++) {
			if (old_set[i] != 0) {
				insert(old_set[i]);
			}
		}
		delete old_set;
	}

	/// <summary>
	/// Circular Shift/Rotate integer by one to the right.
	/// </summary>
	static inline FunctionID rotr(FunctionID f) {
		return (f >> 1) | (f << rotation_mask);
	}

	inline bool try_insert(unsigned int position, const FunctionID f) {
		if (set[position] == f) {
			return true;
		}
		if (set[position] == 0) {
			num_elements++;
			if (num_elements > max_elements) {
				increase_size();
				insert(f);
				return true;
			}

			set[position] = f;
			return true;
		}
		return false;
	}

public:

	~function_id_set() {
		delete set;
	}

	/// <summary>
	/// Empties the set and sets back the size of the underlying array.
	/// </summary>
	void clear() {
		delete set;
		current_size = DEFAULT_SIZE;
		max_elements = DEFAULT_SIZE / 2;
		num_elements = 0;
		set = new FunctionID[DEFAULT_SIZE]{ 0 };
	}

	/// <summary>
	/// Current size of the set.
	/// </summary>
	unsigned int size() {
		return current_size;
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
		unsigned int position = f & modulo_mask;
		if (set[position] == 0) {
			return false;
		}
		if (set[position] == f) {
			return true;
		}

		// Then rotate bits and xor to try and find a new position
		for (unsigned int i = 0; i < NUM_XOR_VALUES; i++) {
			position = (rotr(f) ^ xor_values[i]) & modulo_mask;
			if (set[position] == 0) {
				return false;
			}
			if (set[position] == f) {
				return true;
			}
		}

		// If no position was found, just go to the next position with +1
		position = (f + 1) & modulo_mask;
		while (set[position] != f) {
			if (set[position] == 0) {
				return false;
			}
			position = (position + 1) & modulo_mask;
		}
		return set[position];
	}

	/// <summary>
	/// Inserts FunctionID f into the set.
	/// </summary>
	void insert(FunctionID f) {
		// Try insertion at the number modulo the size of the set first
		unsigned int position = f & modulo_mask;
		if (try_insert(position, f))
		{
			return;
		}

		// Then rotate bits and xor to try and find a new position
		for (unsigned int i = 0; i < NUM_XOR_VALUES; i++) {
			position = (rotr(f) ^ xor_values[i]) & modulo_mask;
			if (try_insert(position, f))
			{
				return;
			}
		}

		// If no position was found, just go to the next position with +1
		position = (f + 1) & modulo_mask;
		while (!try_insert(position, f)) {
			position = (position + 1) & modulo_mask;
		}
	}
};
