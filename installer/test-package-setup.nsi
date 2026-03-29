; =============================================================================
; TestPackage Installer (NSIS)
; =============================================================================
; Installs TestPackage, which lets users design and generate simulated
; installers for testing repackaging and deployment.
; =============================================================================

!include "MUI2.nsh"

; --- Product Info ---
!define PRODUCT_NAME "TestPackage"
!define PRODUCT_VERSION "2.0.0"
!define PRODUCT_PUBLISHER "RWK Systems"
!define PRODUCT_WEB_SITE "https://rwksystems.com"
!define PRODUCT_DIR_REGKEY "Software\Microsoft\Windows\CurrentVersion\App Paths\TestPackageConfigurator.exe"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
!define PRODUCT_UNINST_ROOT_KEY "HKLM"

; --- Installer Settings ---
Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "..\dist\TestPackageSetup.exe"
InstallDir "$PROGRAMFILES\RWK Systems\TestPackage"
InstallDirRegKey HKLM "${PRODUCT_DIR_REGKEY}" ""
RequestExecutionLevel admin
SetCompressor /SOLID lzma

; --- Custom Icon ---
!define MUI_ICON "..\assets\icon.ico"
!define MUI_UNICON "..\assets\icon.ico"

; --- MUI Pages ---
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "$INSTDIR\TestPackageConfigurator.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Launch TestPackage"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

; --- Install Section ---
Section "Install"
    SetOutPath "$INSTDIR"

    ; Core files
    File "..\dist\self-contained\configurator\TestPackageConfigurator.exe"

    ; Template files for generating installers
    SetOutPath "$INSTDIR\templates"
    File "..\dist\self-contained\templates\TestPackageInstaller.exe"
    File "..\dist\self-contained\templates\TestPackageApp.exe"
    File "..\dist\self-contained\templates\config.ini"

    ; Back to main dir
    SetOutPath "$INSTDIR"

    ; Create uninstaller
    WriteUninstaller "$INSTDIR\uninstall.exe"

    ; Shortcuts
    CreateDirectory "$SMPROGRAMS\RWK Systems"
    CreateShortCut "$SMPROGRAMS\RWK Systems\TestPackage.lnk" "$INSTDIR\TestPackageConfigurator.exe" "" "$INSTDIR\TestPackageConfigurator.exe" 0
    CreateShortCut "$SMPROGRAMS\RWK Systems\Uninstall TestPackage.lnk" "$INSTDIR\uninstall.exe"

    ; Registry
    WriteRegStr HKLM "${PRODUCT_DIR_REGKEY}" "" "$INSTDIR\TestPackageConfigurator.exe"
    WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayName" "${PRODUCT_NAME}"
    WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
    WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
    WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
    WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\uninstall.exe"
    WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "InstallLocation" "$INSTDIR"
    WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayIcon" "$INSTDIR\TestPackageConfigurator.exe"
    WriteRegDWORD ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "NoModify" 1
    WriteRegDWORD ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "NoRepair" 1
SectionEnd

; --- Uninstall Section ---
Section "Uninstall"
    ; Remove files
    Delete "$INSTDIR\TestPackageConfigurator.exe"
    Delete "$INSTDIR\templates\TestPackageInstaller.exe"
    Delete "$INSTDIR\templates\TestPackageApp.exe"
    Delete "$INSTDIR\templates\config.ini"
    RMDir "$INSTDIR\templates"
    Delete "$INSTDIR\uninstall.exe"
    RMDir "$INSTDIR"

    ; Remove shortcuts
    Delete "$SMPROGRAMS\RWK Systems\TestPackage.lnk"
    Delete "$SMPROGRAMS\RWK Systems\Uninstall TestPackage.lnk"
    RMDir "$SMPROGRAMS\RWK Systems"

    ; Remove registry
    DeleteRegKey HKLM "${PRODUCT_DIR_REGKEY}"
    DeleteRegKey ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}"
SectionEnd
