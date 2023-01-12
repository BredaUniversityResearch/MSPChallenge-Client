#!groovy

def COLOR_MAP = [
    'SUCCESS': 'good', 
    'FAILURE': 'danger',
]

pipeline {

	agent {
        	node {
            		label 'windows'
		}
	}
	
	stages {
        	stage('Clone Script') {
            		steps {
                		checkout scm
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