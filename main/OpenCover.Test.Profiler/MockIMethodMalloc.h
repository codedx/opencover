#pragma once

#include <corprof.h>
#include <gmock/gmock.h>

class MockMethodMalloc : public IMethodMalloc
{
public:
	virtual ~MockMethodMalloc() {}

	MOCK_METHOD2_WITH_CALLTYPE(__stdcall, QueryInterface, HRESULT(const IID& riid, void** ppvObject));

	MOCK_METHOD0_WITH_CALLTYPE(__stdcall, AddRef, ULONG(void));
	MOCK_METHOD0_WITH_CALLTYPE(__stdcall, Release, ULONG(void));

	MOCK_METHOD1_WITH_CALLTYPE(__stdcall, Alloc, PVOID(ULONG cb));
};