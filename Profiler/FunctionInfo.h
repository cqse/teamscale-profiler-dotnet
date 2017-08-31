#ifndef _FunctionInfo_H_
#define _FunctionInfo_H_

#include <cor.h>

/**
 * Struct that stores information to uniquely identify a function.
 */
struct FunctionInfo {

	/** Index into the assemblyMap of the assembly that contains the function. */
	int assemblyNumber;

	/** Metadata token of the function. */
	mdToken functionToken;
};

#endif
