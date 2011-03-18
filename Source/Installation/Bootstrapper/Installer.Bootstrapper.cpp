/*

 Copyright (c) 2004-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

// Installer.Bootstrapper.cpp : Defines the class behaviors for the application.
//

#include "stdafx.h"
#include "Installer.Bootstrapper.h"
#include "BootstrapperDlg.h"
#include ".\installer.bootstrapper.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


// CBootstrapperApp
BEGIN_MESSAGE_MAP(CBootstrapperApp, CWinApp)
	ON_COMMAND(ID_HELP, CWinApp::OnHelp)
END_MESSAGE_MAP()


// CBootstrapperApp construction
CBootstrapperApp::CBootstrapperApp()
{
	// TODO: add construction code here,
	// Place all significant initialization in InitInstance
}


// The one and only CBootstrapperApp object
CBootstrapperApp theApp;

// 
// Make up some private access rights.
// 
#define ACCESS_READ  1
#define ACCESS_WRITE 2

//**********************************************************************
// 
// FUNCTION:  IsAdmin - This function checks the token of the 
//            calling thread to see if the caller belongs to
//            the Administrators group.
// 
// PARAMETERS:   none
// 
// RETURN VALUE: TRUE if the caller is an administrator on the local
//            machine.  Otherwise, FALSE.
// 
//**********************************************************************
BOOL IsAdmin(void)
{
	HANDLE hToken;
	DWORD  dwStatus;
	DWORD  dwAccessMask;
	DWORD  dwAccessDesired;
	DWORD  dwACLSize;
	DWORD  dwStructureSize = sizeof(PRIVILEGE_SET);
	PACL   pACL            = NULL;
	PSID   psidAdmin       = NULL;
	BOOL   bReturn         = FALSE;

	PRIVILEGE_SET   ps;
	GENERIC_MAPPING GenericMapping;

	PSECURITY_DESCRIPTOR     psdAdmin           = NULL;
	SID_IDENTIFIER_AUTHORITY SystemSidAuthority = SECURITY_NT_AUTHORITY;

	__try {

		// AccessCheck() requires an impersonation token.
		ImpersonateSelf(SecurityImpersonation);

		if (!OpenThreadToken(GetCurrentThread(), TOKEN_QUERY, FALSE, &hToken))
		{
			if (GetLastError() != ERROR_NO_TOKEN) __leave;

			// If the thread does not have an access token, we'll 
			// examine the access token associated with the process.
			if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &hToken)) __leave;
		}

		if (!AllocateAndInitializeSid(&SystemSidAuthority, 2, 
			SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_ADMINS,
			0, 0, 0, 0, 0, 0, &psidAdmin))
			__leave;

		psdAdmin = LocalAlloc(LPTR, SECURITY_DESCRIPTOR_MIN_LENGTH);
		if (psdAdmin == NULL) __leave;

		if (!InitializeSecurityDescriptor(psdAdmin, SECURITY_DESCRIPTOR_REVISION)) __leave;

		// Compute size needed for the ACL.
		dwACLSize = sizeof(ACL) + sizeof(ACCESS_ALLOWED_ACE) + GetLengthSid(psidAdmin) - sizeof(DWORD);

		// Allocate memory for ACL.
		pACL = (PACL)LocalAlloc(LPTR, dwACLSize);
		if (pACL == NULL) __leave;

		// Initialize the new ACL.
		if (!InitializeAcl(pACL, dwACLSize, ACL_REVISION2)) __leave;

		dwAccessMask = ACCESS_READ | ACCESS_WRITE;

		// Add the access-allowed ACE to the DACL.
		if (!AddAccessAllowedAce(pACL, ACL_REVISION2, dwAccessMask, psidAdmin)) __leave;

		// Set the DACL to the SD.
		if (!SetSecurityDescriptorDacl(psdAdmin, TRUE, pACL, FALSE)) __leave;

		// AccessCheck is sensitive about what is in the SD; set
		// the group and owner.
		SetSecurityDescriptorGroup(psdAdmin, psidAdmin, FALSE);
		SetSecurityDescriptorOwner(psdAdmin, psidAdmin, FALSE);

		if (!IsValidSecurityDescriptor(psdAdmin)) __leave;

		dwAccessDesired = ACCESS_READ;

		// 
		// Initialize GenericMapping structure even though you
		// do not use generic rights.
		// 
		GenericMapping.GenericRead    = ACCESS_READ;
		GenericMapping.GenericWrite   = ACCESS_WRITE;
		GenericMapping.GenericExecute = 0;
		GenericMapping.GenericAll     = ACCESS_READ | ACCESS_WRITE;

		if (!AccessCheck(psdAdmin, hToken, dwAccessDesired, 
			&GenericMapping, &ps, &dwStructureSize, &dwStatus, 
			&bReturn))
		{
				printf("AccessCheck() failed with error %lu\n", GetLastError());
				__leave;
		}

		RevertToSelf();

	}
	__finally
	{
		// Clean up.
		if (pACL) LocalFree(pACL);
		if (psdAdmin) LocalFree(psdAdmin);  
		if (psidAdmin) FreeSid(psidAdmin);
	}

	return bReturn;
}

bool IsVC2010()
{
	CRegKey key;
	if (key.Open(HKEY_LOCAL_MACHINE, "SOFTWARE\\Microsoft\\VisualStudio\\10.0\\VC\\VCRedist\\x86", KEY_QUERY_VALUE) != ERROR_SUCCESS)
		return false;

	DWORD installed = 0;
	return (key.QueryDWORDValue("Installed", installed) == ERROR_SUCCESS && installed != 0);
}

// CBootstrapperApp initialization
BOOL CBootstrapperApp::InitInstance()
{
	// check VC++ 2010
	/*if (!IsVC2010())
	{
	::MessageBox(NULL, "This application requires 'Visual C++ 2010 Redistributable (x86)' to be installed first.\n"
	"Please install the prerequisite from the 'Setup' folder and run this setup again."
	, "Phalanger Setup", MB_OK | MB_ICONSTOP);
	return FALSE;
	}*/

	if (!IsAdmin())
	{
		::MessageBox(NULL, "This application must be run under local administrator account.\n"
			"Please log on as an administrator and try again.", "Phalanger Setup", MB_OK | MB_ICONSTOP);
		return FALSE;
	}

	HANDLE mutex = ::CreateMutex(NULL, TRUE, "Phalanger Setup: multiple instances preventing mutex");
	if (mutex != NULL && GetLastError() == ERROR_ALREADY_EXISTS)
	{
		::CloseHandle(mutex);
		::MessageBox(NULL, "Setup is already running.\nPlease finish the installation in progress and try again.",
			"Phalanger Setup", MB_OK | MB_ICONSTOP);
		return FALSE;
	}

	CWinApp::InitInstance();

	AfxEnableControlContainer();

	CBootstrapperDlg dlg;

	m_pMainWnd = &dlg;
	dlg.DoModal();

	::CloseHandle(mutex);

	// Since the dialog has been closed, return FALSE so that we exit the
	// application, rather than start the application's message pump.
	return FALSE;
}

// PreTranslateMessage handler
BOOL CBootstrapperApp::PreTranslateMessage(MSG* pMsg)
{
	// Do not allow right button clicks
	if (pMsg->message == WM_RBUTTONDOWN || pMsg->message == WM_RBUTTONUP) return TRUE;

	return CWinApp::PreTranslateMessage(pMsg);
}

BOOL CBootstrapperApp::OnIdle(LONG lCount)
{
	((CBootstrapperDlg *)m_pMainWnd)->UpdateState();
	return CWinApp::OnIdle(lCount);
}
