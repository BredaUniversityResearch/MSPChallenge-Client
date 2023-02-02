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
		WINDOWS_BUILD_NAME = "Windows-${currentBuild.number}.exe"
		WINDOWS_DEV_BUILD_NAME = "Windows-Dev-${currentBuild.number}.exe"
		MACOS_BUILD_NAME = "MacOS-${currentBuild.number}"
		MACOS_DEV_BUILD_NAME = "MacOS-Dev-${currentBuild.number}"
		RELEASE_FOLDER = "F:\\MSPReleases"
		DEVELOPMENT_FOLDER = "F:\\MSPDevBuilds"
		
		String outputFolder = "CurrentBuild"
		String mac = "mac"
		String windows = "windows"
		String dev = "dev"
		String release = "release"

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
					bat '"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%CD%\\%outputFolder%\\%WINDOWS_DEV_BUILD_NAME%" -customBuildName %WINDOWS_DEV_BUILD_NAME% -executeMethod ProjectBuilder.WindowsDevBuilder'
					
					echo "Cleaning up the Build Folder"
					bat 'del /s /f /q "%CD%\\%outputFolder%"'
				}
			}
		}
		
		stage('Build Dev Branch') {
		
			when { 
					expression { BRANCH_NAME ==~ /(dev)/ }
				}
			steps {
				script {
					echo "Launching App Build..."
					bat '"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%DEVELOPMENT_FOLDER%\\%WINDOWS_DEV_BUILD_NAME%" -customBuildName %WINDOWS_DEV_BUILD_NAME% -executeMethod ProjectBuilder.WindowsDevBuilder'
					bat '"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%DEVELOPMENT_FOLDER%\\%MACOS_DEV_BUILD_NAME%" -customBuildName %MACOS_DEV_BUILD_NAME% -executeMethod ProjectBuilder.MacOSDevBuilder'
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
					slackSend color: COLOR_MAP[currentBuild.currentResult],
					message: "*${currentBuild.currentResult}:* Job ${env.JOB_NAME} build ${env.BUILD_NUMBER}\n More info at: ${env.BUILD_URL}"
			}
		}
}
