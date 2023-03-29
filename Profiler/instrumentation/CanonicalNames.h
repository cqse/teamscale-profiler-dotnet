#pragma once
enum CanonicalName {
#define OPDEF(name, str, decs, incs, args, optp, stdlen, stdop1, stdop2, flow) name,
#include <opcode.def>
#undef OPDEF
};