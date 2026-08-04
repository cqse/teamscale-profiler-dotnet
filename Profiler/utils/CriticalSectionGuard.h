#pragma once
#include <windows.h>

namespace Profiler {
	/**
	 * Enters the given critical section and leaves it again when this object goes out of scope.
	 * Using this instead of EnterCriticalSection/LeaveCriticalSection makes sure that the section is
	 * also left when an exception is thrown.
	 */
	class CriticalSectionGuard {
	public:
		explicit CriticalSectionGuard(CRITICAL_SECTION& section) : section(section) {
			EnterCriticalSection(&this->section);
		}

		~CriticalSectionGuard() {
			LeaveCriticalSection(&this->section);
		}

		CriticalSectionGuard(const CriticalSectionGuard&) = delete;
		CriticalSectionGuard& operator=(const CriticalSectionGuard&) = delete;

	private:
		CRITICAL_SECTION& section;
	};
}
