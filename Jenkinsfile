#!groovy
import org.cradle.Discord
import org.cradle.Signing
import org.cradle.Nexus
import org.cradle.Zip

def COLOR_MAP = [
    'SUCCESS': 'good', 
    'FAILURE': 'danger',
]

pipeline {
    environment {
        // Unity tool installation
        UNITY_EXECUTABLE = "C:\\Program Files\\Unity\\Hub\\Editor\\2022.3.20f1\\Editor\\Unity.exe"
        
        // Latest curl version installation
        CURL_EXECUTABLE = "C:\\Program Files\\Git\\mingw64\\bin\\curl.exe"

        // Unity Build params & paths
        WINDOWS_BUILD_NAME = "Windows-${currentBuild.number}"
        WINDOWS_DEV_BUILD_NAME = "Windows-Dev-${currentBuild.number}"
        MACOS_BUILD_NAME = "MacOS-${currentBuild.number}"
        MACOS_DEV_BUILD_NAME = "MacOS-Dev-${currentBuild.number}"
        WINDOWS_DEV_PR_BUILD_NAME = "Windows-Dev-${BRANCH_NAME}"
        
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
            when {branch comparator: 'REGEXP', pattern: '(bugfix.*|hotfix.*|MSP.*|PR.*)'}
            steps {
                script {
                    echo "Launching Windows Development Build..."
                    bat '''"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%CD%\\%output%\\%outputWinDevFolder%\\MSP-Challenge.exe" -customBuildName "MSP-Challenge" -executeMethod ProjectBuilder.WindowsDevBuilder'''
                    
                    echo "Zipping build..."
                    bat '''7z a -tzip -r "%output%\\%WINDOWS_DEV_PR_BUILD_NAME%" "%CD%\\%output%\\%outputWinDevFolder%\\*"'''
                    
                    echo "Uploading dev build artifact to Nexus..."
                    bat '''"%CURL_EXECUTABLE%" -X POST "http://localhost:8081/service/rest/v1/components?repository=MSPChallenge-Client-PR" -H "accept: application/json" -H "Authorization: Basic %NEXUS_CREDENTIALS%" -F "raw.directory=Windows" -F "raw.asset1=@%output%\\%WINDOWS_DEV_PR_BUILD_NAME%.zip;type=application/x-zip-compressed" -F "raw.asset1.filename=%WINDOWS_DEV_PR_BUILD_NAME%.zip"'''
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
                    bat '''"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%CD%\\%output%\\%outputWinDevFolder%\\MSP-Challenge.exe" -customBuildName "MSP-Challenge" -executeMethod ProjectBuilder.WindowsDevBuilder'''
                    
                    echo "Zipping build..."
                    bat '''7z a -tzip -r "%output%\\%WINDOWS_DEV_BUILD_NAME%" "%CD%\\%output%\\%outputWinDevFolder%\\*"'''
                    
                    echo "Uploading dev build artifact to Nexus..."
                    bat '''"%CURL_EXECUTABLE%" -X POST "http://localhost:8081/service/rest/v1/components?repository=MSPChallenge-Client-Dev" -H "accept: application/json" -H "Authorization: Basic %NEXUS_CREDENTIALS%" -F "raw.directory=Windows" -F "raw.asset1=@%output%\\%WINDOWS_DEV_BUILD_NAME%.zip;type=application/x-zip-compressed" -F "raw.asset1.filename=%WINDOWS_DEV_BUILD_NAME%.zip"'''
                        
                    echo "Launching MacOS Development Build..."
                    bat '''"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%CD%\\%output%\\%outputMacDevFolder%\\MSP-Challenge.app" -customBuildName "MSP-Challenge.app" -executeMethod ProjectBuilder.MacOSDevBuilder'''
                    
                    echo "Zipping build..."
                    bat '''7z a -tzip -r "%output%\\%MACOS_DEV_BUILD_NAME%" "%CD%\\%output%\\%outputMacDevFolder%\\*"'''
                    
                    echo "Uploading dev build artifact to Nexus..."
                    bat '''"%CURL_EXECUTABLE%" -X POST "http://localhost:8081/service/rest/v1/components?repository=MSPChallenge-Client-Dev" -H "accept: application/json" -H "Authorization: Basic %NEXUS_CREDENTIALS%" -F "raw.directory=MacOS" -F "raw.asset1=@%output%\\%MACOS_DEV_BUILD_NAME%.zip;type=application/x-zip-compressed" -F "raw.asset1.filename=%MACOS_DEV_BUILD_NAME%.zip"'''                    
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
                    bat '''"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%CD%\\%output%\\%outputWinFolder%\\MSP-Challenge.exe" -customBuildName "MSP-Challenge" -executeMethod ProjectBuilder.WindowsBuilder'''
                    
                    echo "Zipping build..."
                    bat '''7z a -tzip -r "%output%\\%WINDOWS_BUILD_NAME%" "%CD%\\%output%\\%outputWinFolder%\\*"'''
                    
                    
                    echo "Uploading release build artifact to Nexus..."
                    bat '''"%CURL_EXECUTABLE%" -X POST "http://localhost:8081/service/rest/v1/components?repository=MSPChallenge-Client-Main" -H "accept: application/json" -H "Authorization: Basic %NEXUS_CREDENTIALS%" -F "raw.directory=Windows" -F "raw.asset1=@%output%\\%WINDOWS_BUILD_NAME%.zip;type=application/x-zip-compressed" -F "raw.asset1.filename=%WINDOWS_BUILD_NAME%.zip"'''
                        
                    echo "Launching MacOS Release Build..."
                    bat '''"%UNITY_EXECUTABLE%" -projectPath "%CD%" -quit -batchmode -nographics -customBuildPath "%CD%\\%output%\\%outputMacFolder%\\MSP-Challenge.app" -customBuildName "MSP-Challenge.app" -executeMethod ProjectBuilder.MacOSBuilder'''
                    
                    echo "Zipping build..."
                    bat '''7z a -tzip -r "%output%\\%MACOS_BUILD_NAME%" "%CD%\\%output%\\%outputMacFolder%\\*"'''
                    
                    
                    echo "Uploading release build artifact to Nexus..."
                    bat '''"%CURL_EXECUTABLE%" -X POST "http://localhost:8081/service/rest/v1/components?repository=MSPChallenge-Client-Main" -H "accept: application/json" -H "Authorization: Basic %NEXUS_CREDENTIALS%" -F "raw.directory=MacOS" -F "raw.asset1=@%output%\\%MACOS_BUILD_NAME%.zip;type=application/x-zip-compressed" -F "raw.asset1.filename=%MACOS_BUILD_NAME%.zip"'''
                }
            }
        }
    }
    post {
        always {
            script {
                if(fileExists(output)) {
                    echo "Cleaning up workspace..."
                    bat '''RMDIR %output% /S /Q'''
                }   
                if (currentBuild.result == 'SUCCESS') {
                    Discord.SendMessageToMSPChallengeChannel(this, "MSP-Challenge Build ${currentBuild.number}", "Available on Nexus\n ready for testing:\n [Download from Nexus](https://nexus.cradle.buas.nl/#browse/browse) \n \n Built took: ${currentBuild.durationString}" )
                }
                else {
                    Discord.SendMessageToMSPChallengeChannel(this, "MSP-Challenge Build ${currentBuild.number}", "Build failed\n Reason: ${currentBuild.description}" )
                }
            }
        }
    }
}