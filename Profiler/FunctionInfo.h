#ifndef _FunctionInfo_H_
#define _FunctionInfo_H_

#include <cor.h>

/**
 * Struct that stores information to uniquely identify a function.
 */
struct FunctionInfo {

	/** The assembly ID that contains the function. */
	AssemblyID assemblyID;

	/** Metadata token of the function. */
	mdToken functionToken;
};

#endif
