# Chain installer for InstallShield Setup files
*Small program that can chain run InstallShield setups in silent mode. Ideal for repeated (mass) distribution of InstallShield Setups*

## Included .iss files
Already included .iss files are for installing OMSI 2 (omnibussimulator.de) and paid addons.
If you want to use them, put

## How to use
1. Generate .iss files for your setups by running <code>setup.exe /r /f1"C:\setup.iss"</code>. While you navigate through the dialogues every action will be recorded in the .iss file you specified in the command.
2. Now you need to generate the .iss file for the uninstall process by running <code>setup.exe /uninst /r /f1"C:\setup_uninstall.iss"</code>. Pay attention that you specify the same name as the install file followed by "_uninstall".
3. Put both .iss files into the "iss" directory and the setup file into the "files" directory.
4. (Optional) Open the files and replace any text user input like paths etc. with a variable ('{{myvar}}' for example)
5. (Optional) If you specified a variable in 4, you will need to modify the [variables] section in CHAIN_INSTALL.INI file in the main directory
6. Tell the app which .iss file belongs to which setup executable by editing the [mappings] section.
7. (Optional) Specify a program that should be run when the installation chain has finished in the [other] section.

## Example
Setup files:
* setup1_v201_ms.exe
* setup2_39_SP2.exe

Commands to run:
* <code>setup1_v201_ms.exe /r /f1"C:\setup1_201.iss"</code>
* <code>setup1_v201_ms.exe /uninst /r /f1"C:\setup1_201_uninstall.iss"</code>
* <code>setup2_39_SP2.exe /r /f1"C:\setup2_39_SP2.iss"</code>
* <code>setup2_39_SP2.exe /uninst /r /f1"C:\setup2_39_SP2_uninstall.iss"</code>

Files to move:
* <code>C:\setup1_201.iss</code> **->** <code>Installation_Path\iss\setup1_201.iss</code>
* <code>C:\setup1_201_uninstall.iss</code> **->** <code>Installation_Path\iss\setup1_201_uninstall.iss</code>
* <code>C:\setup2_39_SP2.iss</code> **->** <code>Installation_Path\iss\setup2_39_SP2.iss</code>
* <code>C:\setup2_39_SP2_uninstall.iss</code> **->** <code>Installation_Path\iss\setup2_39_SP2_uninstall.iss</code>
* <code>setup1_v201_ms.exe</code> **->** <code>Installation_Path\files\setup1_v201_ms.exe</code>
* <code>setup2_39_SP2.exe</code> **->** <code>Installation_Path\files\setup2_39_SP2.exe</code>

CHAIN_INSTALL.INI:

    [mappings]
    setup1_v201_ms.exe=setup1_201.iss
    setup2_39_SP2.exe=setup2_39_SP2.iss

    [variables]
    {{INSTALL_DIR}}=C:\Program Files (x86)

    [other]
    launchWhenInstalled=C:\Windows\system32\notepad.exe


***If you have suggestions please implement and submit them as pull requests***
