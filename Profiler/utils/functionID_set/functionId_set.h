#pragma once
#include <corprof.h>
class functionID_set
{
private:
	unsigned int default_size = 2'097'152;
	unsigned int current_size = default_size;
	unsigned int num_elements = 0;
	unsigned int max_elements = default_size / 2;
	FunctionID* set = new FunctionID[default_size] {0};

	unsigned int xor_values[10] = { 2134038170, 2362107340, 3229546752, 1302939050, 200405764, 79516981, 2052331209, 3415124361, 940592490, 430981309 };

	void adjust_size() {
		current_size *= 2;
		max_elements = current_size / 2;
		FunctionID* old_set = set;
		set = new FunctionID[current_size]{ 0 };
		for (unsigned int i = 0; i < current_size / 2; i++) {
			if (old_set[i] != 0) {
				insert(old_set[i]);
			}
		}
		delete old_set;
	}

public:

	~functionID_set()  {
		delete set;
	}

	void clear() {
		delete set;
		current_size = default_size;
		max_elements = default_size / 2;
		num_elements = 0;
		set = new FunctionID[default_size]{ 0 };
	}

	unsigned int size() {
		return current_size;
	}

	FunctionID at(unsigned int i) {
		return set[i];
	}

	bool contains(FunctionID f) {
		unsigned int position = f & (current_size - 1);
		if (set[position] == 0) {
			return false;
		}
		if (set[position] == f) {
			return true;
		}

		for (int i = 0; i < 10; i++) {
			position = (f ^ xor_values[0]) & (current_size - 1);
			if (set[position] == 0) {
				return false;
			}
			if (set[position] == f) {
				return true;
			}
		}

		position = (f + 1) & (current_size - 1);
		while (set[position] != f) {
			if (set[position] == 0) {
				return false;
			}
			position = (position + 1) & (current_size - 1);
		}
		return set[position];
	}


	void insert(FunctionID f) {
		num_elements++;
		if (num_elements > max_elements) {
			adjust_size();
		}
		unsigned int position = f & (current_size - 1);
		if (set[position] == f) {
			return;
		}
		if (set[position] == 0) {
			set[position] = f;
			return;
		}


		for (int i = 0; i < 10; i++) {
			position = (f ^ xor_values[0]) & (current_size - 1);
			if (set[position] == f) {
				return;
			}
			if (set[position] == 0) {
				set[position] = f;
				return;
			}
		}

		position = (f + 1) & (current_size - 1);
		while (set[position] != 0) {
			if (set[position] == f) {
				return;
			}
			position = (position + 1) & (current_size - 1);
		}
		set[position] = f;
	}
};

