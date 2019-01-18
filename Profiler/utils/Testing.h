#pragma once

/**
 * Used to make functions visible to the C++ unit tests.
 * The unit tests link against the profiler Dll so we must explicitly export any
 * functions we wish to call from the unit test code.
 */
#define EXPOSE_TO_CPP_TESTS __declspec(dllexport)