@echo off
if not "%1"=="" (
    set args=/p:PackageVersion=%1 /p:SemanticVersioningV1=false
)
@echo on
.\Build.cmd -Configuration Release -pack %args%