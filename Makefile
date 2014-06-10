MDTOOL ?= /Applications/Xamarin\ Studio.app/Contents/MacOS/mdtool

.PHONY: all clean

all: Refit.dll

Refit.dll:
	mono ./.nuget/NuGet.exe restore Refit-XamarinStudio.sln
	$(MDTOOL) build -c:Release Refit-XamarinStudio.sln

clean:
	rm -rf packages
	$(MDTOOL) build -t:Clean -c:Release Refit-XamarinStudio.sln
