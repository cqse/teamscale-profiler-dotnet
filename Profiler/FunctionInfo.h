 /*
 * @ConQAT.Rating YELLOW Hash: 1FBAEBCDA269A43359F67DC31A376ACE
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
	mdToken funcToken;

};
#endif