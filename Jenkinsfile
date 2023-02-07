#!groovy

def COLOR_MAP = [
	'SUCCESS': 'good', 
	'FAILURE': 'danger',
]

pipeline {
	
	environment {
		// Unity tool installation
		UNITY_EXECUTABLE = "C:\\Program Files\\Unity\\Hub\\Editor\\2020.3.31f1\\Editor\\Unity.exe"

		// Unity Build params & paths
		WINDOWS_BUILD_NAME = "Windows-${currentBuild.number}"
		WINDOWS_DEV_BUILD_NAME = "Windows-Dev-${currentBuild.number}"
		MACOS_BUILD_NAME = "MacOS-${currentBuild.number}"
		MACOS_DEV_BUILD_NAME = "MacOS-Dev-${currentBuild.number}"
		//RELEASE_FOLDER = "F:\\MSPReleases"
		//DEVELOPMENT_FOLDER = "F:\\MSPDevBuilds"
		
		String output = "Output"
		String outputMacFolder = "CurrentMacBuild"
		String outputWinFolder = "CurrentWinBuild"
		String mac = "mac"
		String windows = "windows"
		String dev = "dev"
		String release = "release"
		
		NEXUS_CREDENTIALS = credentials('NEXUS_CREDENTIALS')

		//PARAMETERS DATA
		//IS_DEVELOPMENT_BUILD = "${params.developmentBuild}"
	}
	
	options {
		timestamps()
    }
	
	//parameters {
	//	booleanParam(name: 'developmentBuild', defaultValue: true, description: 'Choose the buildType.')
	//}
	
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
					echo "Launching App Build..."
					bat '"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%CD%\\%output%\\%outputWinFolder%\\%WINDOWS_DEV_BUILD_NAME%.exe" -customBuildName %WINDOWS_DEV_BUILD_NAME% -executeMethod ProjectBuilder.WindowsDevBuilder'
					
					echo "Cleaning up the Build Folder"
					bat 'del /s /f /q "%CD%\\%outputFolder%"'
				}
			}
		}
		
		stage('Build Dev Branch') {
		
			when { 
					expression { BRANCH_NAME ==~ /(JenkinsTest)/ }
				}
			steps {
				script {
					echo "Launching App Build..."
					bat '"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%CD%\\%output%\\%outputWinFolder%\\%WINDOWS_DEV_BUILD_NAME%.exe" -customBuildName %WINDOWS_DEV_BUILD_NAME% -executeMethod ProjectBuilder.WindowsDevBuilder'
					//bat '"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%CD%\\%output%\\%outputMacFolder%\\%MACOS_DEV_BUILD_NAME%" -customBuildName %MACOS_DEV_BUILD_NAME% -executeMethod ProjectBuilder.MacOSDevBuilder'
					
					echo "Zipping builds..."
					bat '7z a -tzip -r "%output%\\%WINDOWS_DEV_BUILD_NAME%" "%CD%\\%output%\\%outputWinFolder%\\*"'
					//bat '7z a -tzip -r "%output%\\%MACOS_DEV_BUILD_NAME%" "%CD%\\%output%\\%outputMacFolder%\\*"'
					
					echo "Uploading builds artifact to Nexus..."
					bat "curl -X 'POST' \
						'http://localhost:8081/service/rest/v1/components?repository=MSPChallenge-Client-Dev' \
						-H 'accept: application/json' \
						-H 'Content-Type: multipart/form-data' \
						-H 'Authorization: Basic %NEXUS_CREDENTIALS%' \
						-F 'raw.directory=MSPChallenge' \
						-F 'raw.asset1=@%output%\\%WINDOWS_DEV_BUILD_NAME%.zip;type=application/x-zip-compressed' \
						-F 'raw.asset1.filename="%WINDOWS_DEV_BUILD_NAME%"'"
						
					//bat "curl -X 'POST' \
					//	'http://localhost:8081/service/rest/v1/components?repository=MSPChallenge-Client-Dev' \
					//	-H 'accept: application/json' \
					//	-H 'Content-Type: multipart/form-data' \
					//	-H 'Authorization: Basic %NEXUS_CREDENTIALS%' \
					//	-F 'raw.directory=MSPChallenge' \
					//	-F 'raw.asset1=@%output%\\%MACOS_DEV_BUILD_NAME%.zip;type=application/x-zip-compressed' \
					//	-F 'raw.asset1.filename="%MACOS_DEV_BUILD_NAME%"'"
					}
			}
		}
		
		stage('Build Main Branch') {
		
			when { 
					expression { BRANCH_NAME ==~ /(main)/ }
				}
			steps {
				script {
					echo "Launching App Build..."
					bat '"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%RELEASE_FOLDER%\\%windows%\\%dev%\\%WINDOWS_DEV_BUILD_NAME%" -customBuildName %WINDOWS_DEV_BUILD_NAME% -executeMethod ProjectBuilder.WindowsDevBuilder'
					bat '"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%RELEASE_FOLDER%\\%mac%\\%dev%\\%MACOS_DEV_BUILD_NAME%" -customBuildName %MACOS_DEV_BUILD_NAME% -executeMethod ProjectBuilder.MacOSDevBuilder'
					bat '"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%RELEASE_FOLDER%\\%windows%\\%release%\\%WINDOWS_BUILD_NAME%" -customBuildName %WINDOWS_BUILD_NAME% -executeMethod ProjectBuilder.WindowsBuilder'
					bat '"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%RELEASE_FOLDER%\\%mac%\\%release%\\%MACOS_BUILD_NAME%" -customBuildName %MACOS_BUILD_NAME% -executeMethod ProjectBuilder.MacOSBuilder'
				}
			}
		}
	}
	post {
			always {
					echo "Cleaning up workspace..."
					bat '''del %output%\\* /F /S /Q'''
					
					slackSend color: COLOR_MAP[currentBuild.currentResult],
					message: "*${currentBuild.currentResult}:* Job ${env.JOB_NAME} build ${env.BUILD_NUMBER}\n More info at: ${env.BUILD_URL}"
			}
		}
}
