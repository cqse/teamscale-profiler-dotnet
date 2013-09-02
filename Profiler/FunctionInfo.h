 /*
 * @ConQAT.Rating YELLOW Hash: A0DA4B561DD23CBB8C93BC2432004EF1
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
	
	/** Metadata token of the class containing the function. */
	mdToken classToken;

	/** Metadata token of the function. */
	mdToken functionToken;
};

#endif
