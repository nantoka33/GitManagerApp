@echo off
chcp 65001 >nul

:: ==== 設定 ====
set PROJECT_NAME=GitManagerApp
set CONFIG=Debug
set FRAMEWORK=net8.0-windows
set OUTPUT_DIR=publish
set ZIP_NAME=%PROJECT_NAME%_ver_1.0.0.zip

:: ==== パス構築 ====
set ROOT_DIR=%~dp0
set BUILD_PATH=%ROOT_DIR%%PROJECT_NAME%\bin\%CONFIG%\%FRAMEWORK%\
set RELEASE_DIR=%ROOT_DIR%release_temp\

:: ==== ビルド確認 ====
if not exist "%BUILD_PATH%" (
    echo ビルドディレクトリが見つかりません: %BUILD_PATH%
    pause
    exit /b
)

:: ==== 一時フォルダにコピー ====
rd /s /q "%RELEASE_DIR%" >nul 2>&1
mkdir "%RELEASE_DIR%"
xcopy /e /i /y "%BUILD_PATH%" "%RELEASE_DIR%" >nul

:: ==== 不要ファイル削除（任意） ====
del "%RELEASE_DIR%*.pdb" >nul 2>&1
del "%RELEASE_DIR%*.xml" >nul 2>&1

:: ==== ZIP作成 ====
powershell -Command "Compress-Archive -Path '%RELEASE_DIR%*' -DestinationPath '%ZIP_NAME%' -Force"


echo リリースZIP作成完了: %ZIP_NAME%
pause
