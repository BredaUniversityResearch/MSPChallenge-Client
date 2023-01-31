::cd Assets/Plugins
::RMDIR /Q PrivatePlugins
::git clone --progress git@github.com:BredaUniversityResearch/MSPChallenge-PrivatePlugins.git
git submodule update --init --recursive --depth 1