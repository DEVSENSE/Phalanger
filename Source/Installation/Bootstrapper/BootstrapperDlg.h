/*

 Copyright (c) 2004-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

// BootstrapperDlg.h : header file
//

#pragma once


// CBootstrapperDlg dialog
class CBootstrapperDlg : public CDHtmlDialog
{
// Construction
public:
	CBootstrapperDlg(CWnd* pParent = NULL);	// standard constructor

	void Launch(CString fileName);
	void UpdateState();

	bool FwInstalled();
	bool VcInstalled();
	bool CoreInstalled();
	bool VsNetInstalled();
	bool VsipInstalled();
	bool IntegrationInstalled();

	void SetFw(UINT img, UINT text);
	void SetCore(UINT img, UINT text);
	void SetVsip(UINT img, UINT text);
	void SetIntegration(UINT img, UINT text);

	static BSTR GetResourceAsBSTR(UINT resourceId);

// Dialog Data
	enum { IDD = IDD_INSTALLERBOOTSTRAPPER_DIALOG, IDH = IDR_HTML_DIALOG };

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support

	HRESULT OnLinkExit(IHTMLElement *pElement);
	HRESULT OnLinkFw(IHTMLElement *pElement);
	HRESULT OnLinkCore(IHTMLElement *pElement);
	HRESULT OnLinkVsip(IHTMLElement *pElement);
	HRESULT OnLinkIntegration(IHTMLElement *pElement);

// Implementation
protected:
	HICON m_hIcon;
	UINT_PTR m_Timer;

	void LogVisit(wchar_t*tag);

	// Generated message map functions
	virtual BOOL OnInitDialog();
	virtual void OnDocumentComplete(LPDISPATCH pDisp, LPCTSTR szUrl);
	afx_msg HCURSOR OnQueryDragIcon();
	DECLARE_MESSAGE_MAP()
	DECLARE_DHTML_EVENT_MAP()
public:
	afx_msg void OnTimer(UINT nIDEvent);
};
