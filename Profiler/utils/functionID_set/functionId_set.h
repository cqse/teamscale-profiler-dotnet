#pragma once
#include <corprof.h>
#include <limits.h>

/// <summary>
/// Set that can only contain functionIDs, which are just unsigned ints.
/// This is based on an array and is a lot faster than the default set implementation of the standard library for this use case.
/// </summary>
class functionID_set
{
private:
	const unsigned int default_size = 2'097'152;
	static const unsigned int rotation_mask = (-1) & (CHAR_BIT * sizeof(default_size) - 1);

	unsigned int current_size = default_size;
	unsigned int num_elements = 0;
	unsigned int max_elements = default_size / 2;
	unsigned int modulo_mask = current_size - 1;
	FunctionID* set = new FunctionID[default_size]{ 0 };


	// Predetermined values for xor operation to place in different spots
	unsigned int xor_values[10] = { 2134038170, 2362107340, 3229546752, 1302939050, 200405764, 79516981, 2052331209, 3415124361, 940592490, 430981309 };
	const unsigned int num_xor_values = sizeof(xor_values) / sizeof(FunctionID);

	/// <summary>
	/// Updates the size of the array and reinserts the element into the new array.
	/// </summary>
	void adjust_size() {
		max_elements = current_size;
		current_size *= 2;
		modulo_mask = current_size - 1;
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
	static inline FunctionID rotr(FunctionID n) {
		return (n >> 1) | (n << rotation_mask);
	}

	inline bool tryInsert(unsigned int position, const FunctionID f) {
		if (set[position] == f) {
			return true;
		}
		if (set[position] == 0) {
			set[position] = f;
			return true;
		}
		return false;
	}

public:

	~functionID_set() {
		delete set;
	}

	/// <summary>
	/// Empties the set and sets back the size of the underlying array.
	/// </summary>
	void clear() {
		delete set;
		current_size = default_size;
		max_elements = default_size / 2;
		num_elements = 0;
		set = new FunctionID[default_size]{ 0 };
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
		for (unsigned int i = 0; i < num_xor_values; i++) {
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
		num_elements++;
		if (num_elements > max_elements) {
			adjust_size();
		}
		unsigned int position = f & modulo_mask;
		if (tryInsert(position, f)) return;

		for (unsigned int i = 0; i < num_xor_values; i++) {
			position = (rotr(f) ^ xor_values[i]) & modulo_mask;
			if (tryInsert(position, f)) return;
		}

		position = (f + 1) & modulo_mask;
		while (set[position] != 0) {
			if (set[position] == f) {
				return;
			}
			position = (position + 1) & modulo_mask;
		}
		set[position] = f;
	}
};
