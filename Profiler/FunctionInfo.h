 /*
 * @ConQAT.Rating GREEN Hash: C0A89B43BC1C023283CD6E4E4CE3C388
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
