#pragma once

#include <cor.h>

namespace Profiler {

	/**
	 * Struct that stores information to uniquely identify a function.
	 */
	struct FunctionInfo {

		/** Index into the assemblyMap of the assembly that contains the function. */
		int assemblyNumber;

		/** Metadata token of the function. */
		mdToken functionToken;
	};
}
