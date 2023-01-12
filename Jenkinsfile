#!groovy

def COLOR_MAP = [
    'SUCCESS': 'good', 
    'FAILURE': 'danger',
]

pipeline {
	
	environment {        
        // Unity tool installation
        UNITY_EXECUTABLE = "C:\\Program Files\\Unity\\Hub\\Editor\\2020.3.31f1\\Editor\\Unity.exe"

        // Unity Build params
        BUILD_NAME = "Windows-${currentBuild.number}"
        String buildTarget = "Win64"
        String outputFolder = "CurrentBuild"

        //PARAMETERS DATA
        IS_DEVELOPMENT_BUILD = "${params.developmentBuild}"

        // Add other EnvVars here
    }
	
	options {
        timestamps()
    }
	
	parameters {
        booleanParam(name: 'developmentBuild', defaultValue: true, description: 'Choose the buildType.')
    }
	
	agent {
        	node {
            		label 'windows'
		}
	}
	
	stages {
        	stage('Clone Script') {
            		steps {
                		checkout scm
						bat '''git submodule init
							git submodule update'''
       		 	}
		}
		
		stage('Build Application') {
			steps {
				script {
					echo "create Application output folder..."
					bat 'mkdir %outputFolder%'

					echo "Buld App..."
					bat '"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -buildTarget "%buildTarget%" -customBuildPath "%CD%\\%outputFolder%\\" -customBuildName %BUILD_NAME% -executeMethod BuildCommand.PerformBuilds'

				}
			}
		}
	}
	post {
        	always {
					bat '''RMDIR "./CurrentBuild"'''
            		slackSend color: COLOR_MAP[currentBuild.currentResult],
                	message: "*${currentBuild.currentResult}:* Job ${env.JOB_NAME} build ${env.BUILD_NUMBER}\n More info at: ${env.BUILD_URL}"
        	}
    	}
}