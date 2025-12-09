[Setup]
AppName=Orion Tools
AppVersion=1.0
; CHANGE 1: Set the main install directory to the SUBFOLDER
DefaultDirName={userappdata}\Autodesk\Revit\Addins\2022\Orion
DefaultGroupName=Orion
OutputBaseFilename=Orion_Installer_2022
Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest

[Files]
; 1. The .addin manifest file 
; CHANGE 2: We send this UP one level ("\..") so it sits in the 'Addins\2022' folder
Source: "C:\Users\User\Documents\Tagayom Files\programming\c#\Orion\Orion.addin"; DestDir: "{app}\.."; Flags: ignoreversion

; 2. The DLL and dependencies 
; CHANGE 3: These now go directly into "{app}" because "{app}" is now the Orion folder
Source: "C:\Users\User\Documents\Tagayom Files\programming\c#\Orion\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Code]
function InitializeSetup(): Boolean;
begin
  // We still check the parent 2022 folder for safety
  if DirExists(ExpandConstant('{userappdata}\Autodesk\Revit\Addins\2022')) then
    Result := True
  else
    begin
      MsgBox('Revit 2022 Addin folder not found! Is Revit 2022 installed?', mbError, MB_OK);
      Result := True; 
    end;
end;