//@Library('CradleSharedLibrary') _ // Loaded implicitly

String Node = ''
String WorkingDir = ''
//Assign a node to run the pipeline
node('WindowsNode') {
    echo "Running on ${env.NODE_NAME} in ${env.WORKSPACE}"
    Node = env.NODE_NAME
    WorkingDir = env.WORKSPACE
}

String windowsBuildName = "Windows"
String windowsDevBuildName = "Windows-Dev"
String macosBuildName = "MacOS"
String macosDevBuildName = "MacOS-Dev"
String windowsDevPRBuildName = "Windows-PR-${BRANCH_NAME}"

String output = "Output"
String outputMacDevFolder = "CurrentMacDevBuild"
String outputWinDevFolder = "CurrentWinDevBuild"
String outputMacFolder = "CurrentMacBuild"
String outputWinFolder = "CurrentWinBuild"

String buildType = ""

String discordWebhook = 'MSP_DISCORD_WEBHOOK'

Boolean cleanupBefore = false
Boolean cleanupAfter = true

String commit = ""
try {
    stage('Clone') {
        if (cleanupBefore) {
            node(Node) {
                dir(WorkingDir) {
                    script {
                        deleteDir()
                    }
                }
                cleanWs()
            }
        }
        node(Node) {
            echo "Checking out branch: ${CHANGE_BRANCH} from GitHub"
            git.checkoutWithSubModules("https://github.com/BredaUniversityResearch/MSPChallenge-Client", "${CHANGE_BRANCH}", 'CRADLE_WEBMASTER_CREDENTIALS')
            commit = git.fetchCommitHash('CRADLE_WEBMASTER_CREDENTIALS')
        }
    }

    stage('Build') {
        //node(Node) {
            script {
                switch (env.BRANCH_NAME) {
                    case ~/(bugfix.*|hotfix.*|MSP.*|PR.*)/:
                        echo "Pull Request Build"
                        buildPR(Node, WorkingDir, output, outputWinDevFolder, windowsDevPRBuildName, commit, discordWebhook)
                        buildType = "PR"
                        break
                    case ~/(dev)/:
                        echo "Dev Build"
                        buildDev(Node, WorkingDir, output, outputWinDevFolder, outputMacDevFolder, windowsDevBuildName, macosDevBuildName, commit, discordWebhook)
                        buildType = "Dev"
                        break
                    case ~/(main)/:
                        echo "Main Build"
                        buildMain(Node, WorkingDir, output, outputWinFolder, outputMacFolder, windowsBuildName, macosBuildName, commit, discordWebhook)
                        buildType = "Main"
                        break
                    default:
                        echo "Regex match failed, building as if Pull Request"
                        buildPR(Node, WorkingDir, output, outputWinDevFolder, windowsDevPRBuildName, commit, discordWebhook)
                        buildType = "PR"
                        //buildDev(Node, WorkingDir, output, outputWinDevFolder, outputMacDevFolder, windowsDevBuildName, macosDevBuildName, commit)
                        //buildMain(Node, WorkingDir, output, outputWinFolder, outputMacFolder, windowsBuildName, macosBuildName, commit)
                        break
                }
            }
        //}
    }
} catch (InterruptedException e) {
    catchError(buildResult: 'ABORTED', stageResult: 'ABORTED') {
        error()
    }
    throw (e)
} catch (Exception e) {
    catchError(buildResult: 'FAILURE', stageResult: 'FAILURE') {
        error()
    }
    node(Node) {
        script {
            discord.failed(discordWebhook, "MSPChallenge-MultiBranch", "${e}")
        }
    }
    throw (e)
} finally {
    try {
        stage('Report-Results') {
            node(Node) {
                script {
                    switch (currentBuild.result) {
                        case "SUCCESS":
                            if (buildType == "PR") {
                                String winZipName = sanitizeinput.buildName(windowsDevPRBuildName, "${currentBuild.number}", commit, "zip")
                                String windowsLink = "https://nexus.cradle.buas.nl/#browse/browse:MSPChallenge-Client-PR:Windows%%2F${winZipName}"
                                discord.succeeded(discordWebhook, "MSPChallenge-MultiBranch-PR", "[Download Windows Build from Nexus](${windowsLink})")
                            } else if (buildType == "Dev") {
                                String winZipName = sanitizeinput.buildName(windowsDevBuildName, "${currentBuild.number}", commit, "zip")
                                String macZipName = sanitizeinput.buildName(macosDevBuildName, "${currentBuild.number}", commit, "zip")
                                String windowsLink = "https://nexus.cradle.buas.nl/#browse/browse:MSPChallenge-Client-Dev:Windows%%2F${winZipName}"
                                String macLink = "https://nexus.cradle.buas.nl/#browse/browse:MSPChallenge-Client-Dev:MacOS%%2F${macZipName}"
                                discord.succeeded(discordWebhook, "MSPChallenge-MultiBranch-Dev", "[Download Windows Build from Nexus](${windowsLink});[Download MacOS Build from Nexus](${macLink})")
                            } else {
                                String winZipName = sanitizeinput.buildName(windowsBuildName, "${currentBuild.number}", commit, "zip")
                                String macZipName = sanitizeinput.buildName(macosBuildName, "${currentBuild.number}", commit, "zip")
                                String windowsLink = "https://nexus.cradle.buas.nl/#browse/browse:MSPChallenge-Client-Main:Windows%%2F${winZipName}"
                                String macLink = "https://nexus.cradle.buas.nl/#browse/browse:MSPChallenge-Client-Main:MacOS%%2F${macZipName}"
                                discord.succeeded(discordWebhook, "MSPChallenge-MultiBranch-Main", "[Download Windows Build from Nexus](${windowsLink});[Download MacOS Build from Nexus](${macLink})")
                            }
                            break
                        case "UNSTABLE":
                            echo "Build was unstable"
                            break
                        case "FAILURE":
                            echo "Build failed"
                            break
                        case "ABORTED":
                            echo "Build was aborted"
                            break
                        default:
                            echo "Unknown result, assuming build was successful"
                            if (buildType == "PR") {
                                String winZipName = sanitizeinput.buildName(windowsDevPRBuildName, "${currentBuild.number}", commit, "zip")
                                String windowsLink = "https://nexus.cradle.buas.nl/#browse/browse:MSPChallenge-Client-PR:Windows%%2F${winZipName}"
                                discord.succeeded(discordWebhook, "MSPChallenge-MultiBranch-PR", "[Download Windows Build from Nexus](${windowsLink})")
                            } else if (buildType == "Dev") {
                                String winZipName = sanitizeinput.buildName(windowsDevBuildName, "${currentBuild.number}", commit, "zip")
                                String macZipName = sanitizeinput.buildName(macosDevBuildName, "${currentBuild.number}", commit, "zip")
                                String windowsLink = "https://nexus.cradle.buas.nl/#browse/browse:MSPChallenge-Client-Dev:Windows%%2F${winZipName}"
                                String macLink = "https://nexus.cradle.buas.nl/#browse/browse:MSPChallenge-Client-Dev:MacOS%%2F${macZipName}"
                                discord.succeeded(discordWebhook, "MSPChallenge-MultiBranch-Dev", "[Download Windows Build from Nexus](${windowsLink});[Download MacOS Build from Nexus](${macLink})")
                            } else {
                                String winZipName = sanitizeinput.buildName(windowsBuildName, "${currentBuild.number}", commit, "zip")
                                String macZipName = sanitizeinput.buildName(macosBuildName, "${currentBuild.number}", commit, "zip")
                                String windowsLink = "https://nexus.cradle.buas.nl/#browse/browse:MSPChallenge-Client-Main:Windows%%2F${winZipName}"
                                String macLink = "https://nexus.cradle.buas.nl/#browse/browse:MSPChallenge-Client-Main:MacOS%%2F${macZipName}"
                                discord.succeeded(discordWebhook, "MSPChallenge-MultiBranch-Main", "[Download Windows Build from Nexus](${windowsLink});[Download MacOS Build from Nexus](${macLink})")
                            }
                            break
                    }
                }
            }
        }
    } catch (InterruptedException e) {
        catchError(buildResult: 'ABORTED', stageResult: 'ABORTED') {
            error()
        }
        throw (e)
    } catch (Exception e) {
        catchError(buildResult: currentBuild.currentResult, stageResult: 'FAILURE') {
            error()
        }
        throw (e)
    } finally {
        stage('Cleanup') {
            if (cleanupAfter) {
                try {
                    node(Node) {
                        dir(WorkingDir) {
                            script {
                                deleteDir()
                            }
                        }
                        cleanWs()
                    }
                } catch (Exception e) {
                    echo "Unexpected failure during cleanup, retrying once..."
                    node(Node) {
                        dir(WorkingDir) {
                            script {
                                deleteDir()
                            }
                        }
                        cleanWs()
                    }
                    throw (e)
                }
            }
        }
    }
}

def buildPR(Node, WorkingDir, output, outputWinDevFolder, buildName, commit, discordWebhook)
{
    stage('WindowsUnityBuild') {
        build job: 'Library/WindowsUnityBuild',
        parameters: [
            string(name: 'NODE', value: Node),
            string(name: 'WORKING_DIR', value: WorkingDir),
            string(name: 'UNITY_VERSION', value: '2022.3.20f1'),
            string(name: 'PROJECTPATH', value: "%CD%"),
            string(name: 'EXPORTPATH', value: "%CD%\\${output}\\${outputWinDevFolder}\\MSP-Challenge.exe"),
            string(name: 'BUILD_NAME', value: 'MSP-Challenge'),
            string(name: 'BUILD_METHOD', value: 'ProjectBuilder.WindowsDevBuilder'),
            string(name: 'DISCORD_WEBHOOK', value: discordWebhook)
        ]
    }
    node(Node) {
        String zipName = sanitizeinput.buildName(buildName, "${currentBuild.number}", commit, "zip")
        stage('ZipWindowsBuild') {
            zip.pack(".\\${output}\\${outputWinDevFolder}", zipName)
        }
        stage('UploadWindowsBuild') {
            nexus.upload("MSPChallenge-Client-PR", zipName, "application/x-zip-compressed", "Windows", 'NEXUS_CREDENTIALS')
        }
        stage('MacOSUnityBuild') {
            catchError(buildResult: 'SUCCESS', stageResult: 'ABORTED') {
                error("Mac Build was skipped")
            }
        }
        stage('ZipMacOSBuild') {
            catchError(buildResult: 'SUCCESS', stageResult: 'ABORTED') {
                error("Mac Zip was skipped")
            }
        }
        stage('UploadMacOSBuild') {
            catchError(buildResult: 'SUCCESS', stageResult: 'ABORTED') {
                error("Mac Upload was skipped")
            }
        }
    }
}

def buildDev(Node, WorkingDir, output, outputWinDevFolder, outputMacDevFolder, windowsDevBuildName, macOSDevBuildName, commit, discordWebhook)
{
    stage('WindowsUnityBuild') {
        build job: 'Library/WindowsUnityBuild',
        parameters: [
            string(name: 'NODE', value: Node),
            string(name: 'WORKING_DIR', value: WorkingDir),
            string(name: 'UNITY_VERSION', value: '2022.3.20f1'),
            string(name: 'PROJECTPATH', value: "%CD%"),
            string(name: 'EXPORTPATH', value: "%CD%\\${output}\\${outputWinDevFolder}\\MSP-Challenge.exe"),
            string(name: 'BUILD_NAME', value: 'MSP-Challenge'),
            string(name: 'BUILD_METHOD', value: 'ProjectBuilder.WindowsDevBuilder'),
            string(name: 'DISCORD_WEBHOOK', value: discordWebhook)
        ]
    }
    node(Node) {
        String winZipName = sanitizeinput.buildName(windowsDevBuildName, "${currentBuild.number}", commit, "zip")
        stage('ZipWindowsBuild') {
            zip.pack(".\\${output}\\${outputWinDevFolder}", winZipName)
        }
        stage('UploadWindowsBuild') {
            nexus.upload("MSPChallenge-Client-Dev", winZipName, "application/x-zip-compressed", "Windows", 'NEXUS_CREDENTIALS')
        }
    }
    stage('MacOSUnityBuild') {
        build job: 'Library/WindowsUnityBuild',
        parameters: [
            string(name: 'NODE', value: Node),
            string(name: 'WORKING_DIR', value: WorkingDir),
            string(name: 'UNITY_VERSION', value: '2022.3.20f1'),
            string(name: 'PROJECTPATH', value: "%CD%"),
            string(name: 'EXPORTPATH', value: "%CD%\\${output}\\${outputMacDevFolder}\\MSP-Challenge.app"),
            string(name: 'BUILD_NAME', value: 'MSP-Challenge.app'),
            string(name: 'BUILD_METHOD', value: 'ProjectBuilder.MacOSDevBuilder'),
            string(name: 'DISCORD_WEBHOOK', value: discordWebhook)
        ]
    }
    node(Node) {
        String macZipName = sanitizeinput.buildName(macOSDevBuildName, "${currentBuild.number}", commit, "zip")
        stage('ZipMacOSBuild') {
            zip.pack(".\\${output}\\${outputMacDevFolder}", macZipName)
        }
        stage('UploadMacOSBuild') {
            nexus.upload("MSPChallenge-Client-Dev", macZipName, "application/x-zip-compressed", "MacOS", 'NEXUS_CREDENTIALS')
        }
    }
}

def buildMain(Node, WorkingDir, output, outputWinFolder, outputMacFolder, windowsBuildName, macOSBuildName, commit, discordWebhook)
{
    stage('WindowsUnityBuild') {
        build job: 'Library/WindowsUnityBuild',
        parameters: [
            string(name: 'NODE', value: Node),
            string(name: 'WORKING_DIR', value: WorkingDir),
            string(name: 'UNITY_VERSION', value: '2022.3.20f1'),
            string(name: 'PROJECTPATH', value: "%CD%"),
            string(name: 'EXPORTPATH', value: "%CD%\\${output}\\${outputWinFolder}\\MSP-Challenge.exe"),
            string(name: 'BUILD_NAME', value: 'MSP-Challenge'),
            string(name: 'BUILD_METHOD', value: 'ProjectBuilder.WindowsBuilder'),
            string(name: 'DISCORD_WEBHOOK', value: discordWebhook)
        ]
    }
    node(Node) {
        String winZipName = sanitizeinput.buildName(windowsBuildName, "${currentBuild.number}", commit, "zip")
        stage('ZipWindowsBuild') {
            zip.pack(".\\${output}\\${outputWinFolder}", winZipName)
        }
        stage('UploadWindowsBuild') {
        nexus.upload("MSPChallenge-Client-Main", winZipName, "application/x-zip-compressed", "Windows", 'NEXUS_CREDENTIALS')
        }
    }
    stage('MacOSUnityBuild') {
        build job: 'Library/WindowsUnityBuild',
        parameters: [
            string(name: 'NODE', value: Node),
            string(name: 'WORKING_DIR', value: WorkingDir),
            string(name: 'UNITY_VERSION', value: '2022.3.20f1'),
            string(name: 'PROJECTPATH', value: "%CD%"),
            string(name: 'EXPORTPATH', value: "%CD%\\${output}\\${outputMacFolder}\\MSP-Challenge.app"),
            string(name: 'BUILD_NAME', value: 'MSP-Challenge.app'),
            string(name: 'BUILD_METHOD', value: 'ProjectBuilder.MacOSBuilder'),
            string(name: 'DISCORD_WEBHOOK', value: discordWebhook)
        ]
    }
    node(Node) {
        String macZipName = sanitizeinput.buildName(macOSBuildName, "${currentBuild.number}", commit, "zip")
        stage('ZipMacOSBuild') {
            zip.pack(".\\${output}\\${outputMacFolder}", macZipName)
        }
        stage('UploadMacOSBuild') {
            nexus.upload("MSPChallenge-Client-Main", macZipName, "application/x-zip-compressed", "MacOS", 'NEXUS_CREDENTIALS')
        }
    }
}
