#!/bin/bash
# Builds AntiBacteria with the installed Xcode and saves the log to build.log
cd "$(dirname "$0")"
xcodebuild -project AntiBacteria.xcodeproj -scheme AntiBacteria -configuration Debug \
  -derivedDataPath build CODE_SIGN_IDENTITY="-" CODE_SIGNING_ALLOWED=NO build 2>&1 | tee build.log | grep -E "error:|warning: .*(deprecated|unsupported)|BUILD (SUCCEEDED|FAILED)" | sort -u | head -80
echo "Full log: $(pwd)/build.log"
