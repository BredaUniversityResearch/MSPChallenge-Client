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
        BUILD_NAME = "Windows-${currentBuild.number}.exe"
        String buildTarget = "StandaloneWindows64"
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
						echo "Cloning the branch commit"
						checkout scm
						echo "Cloning the private plugins"
						bat '''BuildScripts/CloneSubmodule.bat'''
						//withCredentials([sshUserPrivateKey(credentialsId: 'CradleBuildMachine',  keyFileVariable: 'CradleBuildMachine')]) {
						//	bat '''BuildScripts/CloneSubmodule.bat'''
						//}						
						//bat '''cd Assets/Plugins
						//	rmdir /s /q ./PrivatePlugins
						//	git clone -b main git@github.com:BredaUniversityResearch/MSPChallenge-PrivatePlugins.git'''
						//bat	'''git submodule update --recursive --init'''
						//	git checkout JenkinsTest'''
						
						//checkout([$class: 'GitSCM',
						//	branches: [[name: '*/JenkinsTest']],
						//	doGenerateSubmoduleConfigurations: false,
						//	extensions: [[$class: 'SubmoduleOption',
						//		disableSubmodules: false,
						//		parentCredentials: true,
						//		recursiveSubmodules: true,
						//		reference: '',
						//		trackingSubmodules: false]], 
						//	submoduleCfg: [], 
						//	userRemoteConfigs: [[credentialsId: 'WebmasterCradle', url: 'ssh://git@github.com:BredaUniversityResearch/MSPChallenge-PrivatePlugins.git']]])
       		 	}
		}
		
		stage('Build Application') {
			steps {
				script {
					echo "create Application output folder..."
					//bat 'mkdir %outputFolder%'

					echo "Buld App..."
					bat '"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -buildTarget "%buildTarget%" -customBuildPath "%CD%\\%outputFolder%\\%BUILD_NAME%" -customBuildName %BUILD_NAME% -executeMethod ProjectBuilder.TestBuildJenkins'
				}
			}
		}
	}
	post {
        	always {
					//bat '''RMDIR %outputFolder%'''
            		slackSend color: COLOR_MAP[currentBuild.currentResult],
                	message: "*${currentBuild.currentResult}:* Job ${env.JOB_NAME} build ${env.BUILD_NUMBER}\n More info at: ${env.BUILD_URL}"
        	}
    	}
}