 /*
 * @ConQAT.Rating YELLOW Hash: 52F4D0EF0460AFAFE2308D10697E10AB
 */

#ifndef _MethodInfo_H_
#define _MethodInfo_H_

using namespace std;

/**
 * Struct that stores information to uniquely identify a method.
 */
struct MethodInfo {

	int assemblyNumber;
	mdToken classToken;
	mdToken funcToken;

};
#endif