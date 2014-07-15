:START

del /F /S /Q *.tmp
del /F /S /Q *.bak
del /F /S /Q *.ini
del /F /S /Q *.user
del /F /S /Q *.pdb
del /F /S /Q *.pdb
del /F /S /Q *.vshost.exe
del /F /S /Q *.vshost.exe.manifest
#del /F /S /Q /AH *.suo

rd /S /Q AutoUpdater\obj
rd /S /Q CrashReporter\obj
rd /S /Q ObjectListView\obj
rd /S /Q Evaluator\obj
rd /S /Q FontBuilder\obj
rd /S /Q MP3Player\obj
rd /S /Q WAVMaker\obj
rd /S /Q Touch\obj
rd /S /Q Version\obj

rd /S /Q AutoUpdater\bin
rd /S /Q CrashReporter\bin
rd /S /Q ObjectListView\bin
rd /S /Q Evaluator\bin
rd /S /Q FontBuilder\bin
rd /S /Q MP3Player\bin
rd /S /Q WAVMaker\bin
rd /S /Q Touch\bin
rd /S /Q Version\bin

:END
