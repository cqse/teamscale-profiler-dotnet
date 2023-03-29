#pragma once

#include "CanonicalNames.h"
#include <vector>
#include <Windows.h>

#define UNSAFE_BRANCH_OPERAND 0xDEADBABE
class Instruction;
class Method;

typedef std::vector<Instruction*> InstructionList;

/// <summary>A representation of an IL instruction.</summary>
class Instruction
{
public:
	Instruction(CanonicalName operation, ULONGLONG operand);
	explicit Instruction(CanonicalName operation);
	ULONGLONG m_operand;

protected:
	Instruction();
	Instruction& operator = (const Instruction& b);
	bool Equivalent(const Instruction& b);
	long m_offset;
	CanonicalName m_operation;
	bool m_isBranch;

	std::vector<long> m_branchOffsets;
	InstructionList m_branches;
	InstructionList m_joins;

	long m_origOffset;

public:

	friend class Method;
};
