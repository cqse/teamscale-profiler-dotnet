 /*
 * @ConQAT.Rating YELLOW Hash: B49E4D0FB4D08E549DB72B86E277EDAB
 */

#ifndef _FunctionInfo_H_
#define _FunctionInfo_H_

using namespace std;

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
