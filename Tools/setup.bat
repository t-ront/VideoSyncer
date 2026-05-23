@echo off
setlocal
set PYTHON_DIR=%~dp0python
set PYTHON_EXE=%PYTHON_DIR%\python.exe
set PYTHON_VER=3.11.9
set ZIP_NAME=python_embed.zip



echo [Check] Environment Check...

if exist "%PYTHON_EXE%" (
    echo [+] Local Python found.
    goto :pip_install
)

echo [!] Portable Python not found. Installing...

echo [+] Downloading Python %PYTHON_VER%...
powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest https://www.python.org/ftp/python/%PYTHON_VER%/python-%PYTHON_VER%-embed-amd64.zip -OutFile '%ZIP_NAME%'"

echo [+] Extracting...
powershell -Command "Expand-Archive -Path '%ZIP_NAME%' -DestinationPath '%PYTHON_DIR%'"
del "%ZIP_NAME%"

echo [+] Applying settings...
pushd "%PYTHON_DIR%"
for %%f in (python*._pth) do echo import site >> "%%f"
popd

:pip_install
if not exist "%PYTHON_DIR%\get-pip.py" (
    echo [+] Downloading pip manager...
    powershell -Command "Invoke-WebRequest https://bootstrap.pypa.io/get-pip.py -OutFile '%PYTHON_DIR%\get-pip.py'"
)

echo [+] Installing libraries. Please wait...

"%PYTHON_EXE%" "%PYTHON_DIR%\get-pip.py" --target "%PYTHON_DIR%" --no-warn-script-location
"%PYTHON_EXE%" -m pip install requests numpy scipy soundfile --target "%PYTHON_DIR%" --no-warn-script-location

echo.
echo --------------------------------------------------
echo Setup Complete!
echo --------------------------------------------------
pause