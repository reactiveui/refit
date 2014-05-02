MDTOOL ?= /Applications/Xamarin\ Studio.app/Contents/MacOS/mdtool

.PHONY: all clean

all: Refit.dll

Refit.dll:
	$(MDTOOL) build -c:Release Refit-XamarinStudio.sln

clean:
	$(MDTOOL) build -t:Clean -c:Release Refit-XamarinStudio.sln
