; =============================================================================
; Test Package NSIS Installer Script
; =============================================================================
; Builds a standard Windows installer for the Test Package tool.
; Installs to: %ProgramFiles%\RWK Systems\Test Package
;
; Build this with:
;   makensis installer\test-package-setup.nsi
;
; The script expects the self-contained build output in dist\self-contained\
; =============================================================================

!include "MUI2.nsh"
!include "FileFunc.nsh"

; ---------------------------------------------------------------------------
; General
; ---------------------------------------------------------------------------
Name "Test Package"
OutFile "..\dist\TestPackageSetup.exe"
InstallDir "$PROGRAMFILES\RWK Systems\Test Package"
InstallDirRegKey HKLM "Software\RWK Systems\Test Package" "InstallDir"
RequestExecutionLevel admin
Unicode True

; Version info
!define PRODUCT_NAME "Test Package"
!define PRODUCT_VERSION "1.0.2"
!define PRODUCT_PUBLISHER "RWK Systems"
!define PRODUCT_WEB_SITE "https://rwksystems.com"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
!define PRODUCT_UNINST_ROOT_KEY "HKLM"

VIProductVersion "1.0.2.0"
VIAddVersionKey "ProductName" "${PRODUCT_NAME}"
VIAddVersionKey "CompanyName" "${PRODUCT_PUBLISHER}"
VIAddVersionKey "LegalCopyright" "Copyright ${PRODUCT_PUBLISHER}"
VIAddVersionKey "FileDescription" "${PRODUCT_NAME} Setup"
VIAddVersionKey "FileVersion" "${PRODUCT_VERSION}"
VIAddVersionKey "ProductVersion" "${PRODUCT_VERSION}"

; ---------------------------------------------------------------------------
; Interface Settings
; ---------------------------------------------------------------------------
!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

; Welcome page
!define MUI_WELCOMEPAGE_TITLE "Welcome to Test Package Setup"
!define MUI_WELCOMEPAGE_TEXT "This wizard will install Test Package on your computer.$\r$\n$\r$\nTest Package is a configurable Windows installation simulator for testing repackaging tools, application virtualization solutions, and deployment platforms.$\r$\n$\r$\nClick Next to continue."

; Finish page
!define MUI_FINISHPAGE_TITLE "Test Package Setup Complete"
!define MUI_FINISHPAGE_TEXT "Test Package has been installed on your computer.$\r$\n$\r$\nYou can now run test-package.exe to launch the installation simulator. Edit config.ini to customize the installation behavior before running."
!define MUI_FINISHPAGE_RUN ""
!define MUI_FINISHPAGE_RUN_TEXT "Open installation folder"
!define MUI_FINISHPAGE_RUN_FUNCTION "OpenInstallFolder"
!define MUI_FINISHPAGE_SHOWREADME "$INSTDIR\readme.txt"
!define MUI_FINISHPAGE_SHOWREADME_TEXT "View readme"

; ---------------------------------------------------------------------------
; Pages
; ---------------------------------------------------------------------------
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

; Uninstaller pages
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; ---------------------------------------------------------------------------
; Languages
; ---------------------------------------------------------------------------
!insertmacro MUI_LANGUAGE "English"

; ---------------------------------------------------------------------------
; Install Section
; ---------------------------------------------------------------------------
Section "Install" SecInstall
    SetOutPath "$INSTDIR"

    ; Install all files from the self-contained build
    File /r "..\dist\self-contained\*.*"

    ; Ensure key files are present (these should already be in the build output)
    ; test-package.exe, TestPackageApp.exe, config.ini are from the dotnet publish
    ; readme.txt is copied during build

    ; Create uninstaller
    WriteUninstaller "$INSTDIR\uninstall.exe"

    ; Registry entries for Add/Remove Programs
    WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayName" "${PRODUCT_NAME}"
    WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "UninstallString" '"$INSTDIR\uninstall.exe"'
    WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "QuietUninstallString" '"$INSTDIR\uninstall.exe" /S'
    WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "InstallLocation" "$INSTDIR"
    WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayIcon" "$INSTDIR\test-package.exe"
    WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
    WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
    WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
    WriteRegDWORD ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "NoModify" 1
    WriteRegDWORD ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "NoRepair" 1

    ; Calculate installed size
    ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
    IntFmt $0 "0x%08X" $0
    WriteRegDWORD ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "EstimatedSize" $0

    ; Store install directory
    WriteRegStr HKLM "Software\RWK Systems\Test Package" "InstallDir" "$INSTDIR"

    ; Create Start Menu shortcuts
    CreateDirectory "$SMPROGRAMS\RWK Systems"
    CreateShortcut "$SMPROGRAMS\RWK Systems\Test Package.lnk" "$INSTDIR\test-package.exe"
    CreateShortcut "$SMPROGRAMS\RWK Systems\Uninstall Test Package.lnk" "$INSTDIR\uninstall.exe"
SectionEnd

; ---------------------------------------------------------------------------
; Uninstall Section
; ---------------------------------------------------------------------------
Section "Uninstall"
    ; Remove Start Menu shortcuts
    Delete "$SMPROGRAMS\RWK Systems\Test Package.lnk"
    Delete "$SMPROGRAMS\RWK Systems\Uninstall Test Package.lnk"
    RMDir "$SMPROGRAMS\RWK Systems"

    ; Remove registry entries
    DeleteRegKey ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}"
    DeleteRegKey HKLM "Software\RWK Systems\Test Package"

    ; Remove files and directory
    RMDir /r "$INSTDIR"
SectionEnd

; ---------------------------------------------------------------------------
; Functions
; ---------------------------------------------------------------------------
Function OpenInstallFolder
    ExecShell "open" "$INSTDIR"
FunctionEnd

