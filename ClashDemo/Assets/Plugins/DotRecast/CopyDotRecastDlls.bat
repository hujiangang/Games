@echo off
chcp 65001 >nul
setlocal

:: ====== 可配置项 ======
set "CFG=Debug"
set "NET=netstandard2.1"

:: 根目录
set "ROOT=D:\work\physics\DotRecast\src"
:: 目标目录
set "DEST=D:\work\UnityProject\Games\ClashDemo\Assets\Plugins\DotRecast"

echo ==============================================
echo CFG=%CFG%  NET=%NET%
echo 源根目录: %ROOT%
echo 目标目录: %DEST%
echo ==============================================
echo.

:: 确保目标目录存在
if not exist "%DEST%" (
    echo [创建目录] %DEST%
    md "%DEST%"
)

:: 定义四个dll的完整路径
set "P1=%ROOT%\DotRecast.Core\bin\%CFG%\%NET%\DotRecast.Core.dll"
set "P2=%ROOT%\DotRecast.Recast\bin\%CFG%\%NET%\DotRecast.Recast.dll"
set "P3=%ROOT%\DotRecast.Detour\bin\%CFG%\%NET%\DotRecast.Detour.dll"
set "P4=%ROOT%\DotRecast.Detour.Crowd\bin\%CFG%\%NET%\DotRecast.Detour.Crowd.dll"

:: 逐个拷贝并打印路径
call :copyFile "%P1%"
call :copyFile "%P2%"
call :copyFile "%P3%"
call :copyFile "%P4%"

echo.
echo ===== 全部处理完成 =====
pause
endlocal
exit /b

:: 拷贝子函数：打印路径+判断存在+拷贝
:copyFile
set "SRC=%~1"
echo [检查] %SRC%
if exist "%SRC%" (
    echo [拷贝] "%SRC%" ^> "%DEST%\"
    copy "%SRC%" "%DEST%\" /Y >nul
) else (
    echo [缺失] "%SRC%"
)
echo.