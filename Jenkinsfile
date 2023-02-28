#!groovy

def COLOR_MAP = [
	'SUCCESS': 'good', 
	'FAILURE': 'danger',
]

pipeline {
	
	environment {
		// Unity tool installation
		UNITY_EXECUTABLE = "C:\\Program Files\\Unity\\Hub\\Editor\\2020.3.31f1\\Editor\\Unity.exe"
		
		// Latest curl version installation
		CURL_EXECUTABLE = "C:\\Program Files\\Git\\mingw64\\bin\\curl.exe"

		// Unity Build params & paths
		WINDOWS_BUILD_NAME = "Windows-${currentBuild.number}"
		WINDOWS_DEV_BUILD_NAME = "Windows-Dev-${currentBuild.number}"
		MACOS_BUILD_NAME = "MacOS-${currentBuild.number}"
		MACOS_DEV_BUILD_NAME = "MacOS-Dev-${currentBuild.number}"
		
		String output = "Output"
		String outputMacDevFolder = "CurrentMacDevBuild"
		String outputWinDevFolder = "CurrentWinDevBuild"
		String outputMacFolder = "CurrentMacBuild"
		String outputWinFolder = "CurrentWinBuild"
		
		NEXUS_CREDENTIALS = credentials('NEXUS_CREDENTIALS')
	}
	
	options {
		timestamps()
    }
	
	agent {
			node {
					label 'windows'
		}
	}
	
	stages {
			stage('Clone Script') {
					steps {
						echo "Cloning the branch commit"
						checkout scm
						echo "Fetching tags"
						bat '''git fetch --all --tags'''
				}
		}
		
		stage('Build Pull Request') {
		
			when { 
					expression { BRANCH_NAME ==~ /(MSP-[0-9]+)/ }
				}
			steps {
				script {
					echo "Launching Windows Development Build..."
					bat '''"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%CD%\\%output%\\%outputWinDevFolder%\\\MSP Challenge.exe" -customBuildName "MSP Challenge" -executeMethod ProjectBuilder.WindowsDevBuilder'''
				}
			}
		}
		
		stage('Build Dev Branch') {
		
			when { 
					expression { BRANCH_NAME ==~ /(dev)/ }
				}
			steps {
				script {
					echo "Launching Windows Development Build..."
					bat '''"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%CD%\\%output%\\%outputWinDevFolder%\\MSP Challenge.exe" -customBuildName "MSP Challenge" -executeMethod ProjectBuilder.WindowsDevBuilder
						echo "Zipping build..."
						7z a -tzip -r "%output%\\%WINDOWS_DEV_BUILD_NAME%" "%CD%\\%output%\\%outputWinDevFolder%\\*"
						echo "Uploading dev build artifact to Nexus..."
						"%CURL_EXECUTABLE%" -X POST "http://localhost:8081/service/rest/v1/components?repository=MSPChallenge-Client-Dev" -H "accept: application/json" -H "Authorization: Basic %NEXUS_CREDENTIALS%" -F "raw.directory=Windows" -F "raw.asset1=@%output%\\%WINDOWS_DEV_BUILD_NAME%.zip;type=application/x-zip-compressed" -F "raw.asset1.filename=%WINDOWS_DEV_BUILD_NAME%"'''
						
					echo "Launching MacOS Development Build..."
					bat '''"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%CD%\\%output%\\%outputMacDevFolder%\\MSP Challenge" -customBuildName "MSP Challenge" -executeMethod ProjectBuilder.MacOSDevBuilder
						echo "Zipping build..."
						7z a -tzip -r "%output%\\%MACOS_DEV_BUILD_NAME%" "%CD%\\%output%\\%outputMacDevFolder%\\*"
						echo "Uploading dev build artifact to Nexus..."
						"%CURL_EXECUTABLE%" -X POST "http://localhost:8081/service/rest/v1/components?repository=MSPChallenge-Client-Dev" -H "accept: application/json" -H "Authorization: Basic %NEXUS_CREDENTIALS%" -F "raw.directory=MacOS" -F "raw.asset1=@%output%\\%MACOS_DEV_BUILD_NAME%.zip;type=application/x-zip-compressed" -F "raw.asset1.filename=%MACOS_DEV_BUILD_NAME%"'''					
					}
			}
		}
		
		stage('Build Main Branch') {
		
			when { 
					expression { BRANCH_NAME ==~ /(main)/ }
				}
			steps {
				script {
					
					echo "Launching Windows Release Build..."
					bat '''"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%CD%\\%output%\\%outputWinFolder%\\MSP Challenge.exe" -customBuildName "MSP Challenge" -executeMethod ProjectBuilder.WindowsBuilder
						echo "Zipping build..."
						7z a -tzip -r "%output%\\%WINDOWS_BUILD_NAME%" "%CD%\\%output%\\%outputDevFolder%\\*"
						echo "Uploading release build artifact to Nexus..."
						"%CURL_EXECUTABLE%" -X POST "http://localhost:8081/service/rest/v1/components?repository=MSPChallenge-Client-Main" -H "accept: application/json" -H "Authorization: Basic %NEXUS_CREDENTIALS%" -F "raw.directory=Windows" -F "raw.asset1=@%output%\\%WINDOWS_BUILD_NAME%.zip;type=application/x-zip-compressed" -F "raw.asset1.filename=%WINDOWS_BUILD_NAME%"'''
						
					echo "Launching MacOS Release Build..."
					bat '''"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%CD%\\%output%\\%outputMacFolder%\\MSP Challenge" -customBuildName "MSP Challenge" -executeMethod ProjectBuilder.MacOSBuilder
						echo "Zipping build..."
						7z a -tzip -r "%output%\\%MACOS_BUILD_NAME%" "%CD%\\%output%\\%outputMacFolder%\\*"
						echo "Uploading release build artifact to Nexus..."
						"%CURL_EXECUTABLE%" -X POST "http://localhost:8081/service/rest/v1/components?repository=MSPChallenge-Client-Main" -H "accept: application/json" -H "Authorization: Basic %NEXUS_CREDENTIALS%" -F "raw.directory=MacOS" -F "raw.asset1=@%output%\\%MACOS_BUILD_NAME%.zip;type=application/x-zip-compressed" -F "raw.asset1.filename=%MACOS_BUILD_NAME%"'''
				}
			}
		}
	}
	post {
			always {
					echo "Cleaning up workspace..."
					bat '''RMDIR %output% /S /Q'''
					
					slackSend color: COLOR_MAP[currentBuild.currentResult],
					message: "*${currentBuild.currentResult}:* Job ${env.JOB_NAME} build ${env.BUILD_NUMBER}\n More info at: ${env.BUILD_URL}"
			}
		}
}
