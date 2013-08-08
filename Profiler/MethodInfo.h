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