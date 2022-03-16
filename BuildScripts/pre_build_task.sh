# restore nuget packages

"$UNITY_EXE" -quit -batchmode -ignoreCompilerErrors -projectPath "$WORKSPACE" -executeMethod NugetForUnity.NugetHelper.Restore
