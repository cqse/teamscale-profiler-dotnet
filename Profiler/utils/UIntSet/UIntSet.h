#pragma once
#include <corprof.h>
#include <limits.h>

/// <summary>
/// Set that can only contain UINT64, which are just unsigned ints.
/// This is based on an array and is a lot faster than the default set implementation of the standard library for this use case.
/// </summary>
class UIntSet
{
private:
	const unsigned int default_size = 2'097'152;
	static const UINT64 rotation_mask = (-1) & (CHAR_BIT * sizeof(UINT64) - 1);

	unsigned int current_size = default_size;
	unsigned int num_elements = 0;
	unsigned int max_elements = default_size / 2;
	unsigned int modulo_mask = current_size - 1;
	UINT64* set = new UINT64[default_size]{ 0 };

	bool resizing = false;


	// Predetermined values for xor operation to place in different spots
	UINT64 xor_values[10] = { 14653048717414601650, 10827059576848633699, 18275351206681430557, 15388157215360276699, 5625538261940547542, 17184451615065543950, 12483508915876842786, 7009288139810362683, 8675975670288616007, 11886397353506492918 };

	const unsigned int num_xor_values = sizeof(xor_values) / sizeof(UINT64);

	/// <summary>
	/// Updates the size of the array and reinserts the element into the new array.
	/// </summary>
	void adjust_size() {
		max_elements = current_size;
		current_size *= 2;
		modulo_mask = current_size - 1;
		num_elements = 0;
		UINT64* old_set = set;
		set = new UINT64[current_size]{ 0 };
		for (unsigned int index = 0; index < max_elements; index++) {
			if (old_set[index] != 0) {
				insert(old_set[index]);
			}
		}
		delete old_set;
	}

	/// <summary>
	/// Circular Shift/Rotate integer by one to the right.
	/// </summary>
	static inline UINT64 rotr(UINT64 f) {
		return (f >> 1) | (f << rotation_mask);
	}

	inline bool tryInsert(unsigned int position, const UINT64 i) {
		if (set[position] == i) {
			return true;
		}
		if (set[position] == 0) {
			num_elements++;
			if (num_elements > max_elements) {
				adjust_size();
			}

			set[position] = i;
			return true;
		}
		return false;
	}

public:

	~UIntSet() {
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
		set = new UINT64[default_size]{ 0 };
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
	UINT64 at(unsigned int index) {
		return set[index];
	}

	/// <summary>
	/// True if the set contains UINT64 f, false otherwise.
	/// </summary>
	bool contains(UINT64 i) {
		// Check the number modulo the size of the set first
		unsigned int position = i & modulo_mask;
		if (set[position] == 0) {
			return false;
		}
		if (set[position] == i) {
			return true;
		}

		// Then rotate bits and xor to try and find a new position
		for (unsigned int i = 0; i < num_xor_values; i++) {
			position = (rotr(i) ^ xor_values[i]) & modulo_mask;
			if (set[position] == 0) {
				return false;
			}
			if (set[position] == i) {
				return true;
			}
		}

		// If no position was found, just go to the next position with +1
		position = (i + 1) & modulo_mask;
		while (set[position] != i) {
			if (set[position] == 0) {
				return false;
			}
			position = (position + 1) & modulo_mask;
		}
		return set[position];
	}

	

	/// <summary>
	/// Inserts UINT64 f into the set.
	/// </summary>
	void insert(UINT64 i) {
		// Try insertion at the number modulo the size of the set first
		unsigned int position = i & modulo_mask;
		if (tryInsert(position, i)) return;

		// Then rotate bits and xor to try and find a new position
		for (unsigned int i = 0; i < num_xor_values; i++) {
			position = (rotr(i) ^ xor_values[i]) & modulo_mask;
			if (tryInsert(position, i)) return;
		}

		// If no position was found, just go to the next position with +1
		position = (i + 1) & modulo_mask;
		while (!tryInsert(position, i)) {
			position = (position + 1) & modulo_mask;
		}
	}
};
