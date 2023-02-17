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
		MACOS_BUILD_NAME = "MacOS-${currentBuild.number}"
		
		String output = "Output"
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
		
		stage('Build Temporary Release') {
		
			steps {
				script {
					
					echo "Launching Windows Release Build..."
					bat '''"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%CD%\\%output%\\%outputWinFolder%\\%WINDOWS_BUILD_NAME%.exe" -customBuildName %WINDOWS_BUILD_NAME% -executeMethod ProjectBuilder.WindowsBuilder
						echo "Zipping build..."
						7z a -tzip -r "%output%\\%WINDOWS_BUILD_NAME%" "%CD%\\%output%\\%outputWinFolder%\\*"
						echo "Uploading release build artifact to Nexus..."
						"%CURL_EXECUTABLE%" -X POST "http://localhost:8081/service/rest/v1/components?repository=MSPChallenge-Temporary-Releases" -H "accept: application/json" -H "Authorization: Basic %NEXUS_CREDENTIALS%" -F "raw.directory=MSPChallenge" -F "raw.asset1=@%output%\\%WINDOWS_BUILD_NAME%.zip;type=application/x-zip-compressed" -F "raw.asset1.filename=%WINDOWS_BUILD_NAME%"'''
						
					echo "Launching MacOS Release Build..."
					bat '''"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%CD%\\%output%\\%outputMacFolder%\\%MACOS_BUILD_NAME%" -customBuildName %MACOS_BUILD_NAME% -executeMethod ProjectBuilder.MacOSBuilder
						echo "Zipping build..."
						7z a -tzip -r "%output%\\%MACOS_BUILD_NAME%" "%CD%\\%output%\\%outputMacFolder%\\*"
						echo "Uploading release build artifact to Nexus..."
						"%CURL_EXECUTABLE%" -X POST "http://localhost:8081/service/rest/v1/components?repository=MSPChallenge-Temporary-Releases" -H "accept: application/json" -H "Authorization: Basic %NEXUS_CREDENTIALS%" -F "raw.directory=MSPChallenge" -F "raw.asset1=@%output%\\%MACOS_BUILD_NAME%.zip;type=application/x-zip-compressed" -F "raw.asset1.filename=%MACOS_BUILD_NAME%"'''
						
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