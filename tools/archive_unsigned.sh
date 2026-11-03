#!/usr/bin/env bash
# Unsigned archive of the sts2 iOS app, with the xcodebuild output kept in build/archive.log.
#
# The log matters: the GitHub job log needs a token to read, so the workflow publishes this file
# to the release (and as a check-run annotation) when the archive fails. build/ does not exist
# before this step runs, so create it here - `tee` into a missing directory is itself a failure
# and hides xcodebuild's real error.
#
# Usage: tools/archive_unsigned.sh <bundle-id>
set -o pipefail

BUNDLE_ID="${1:?usage: archive_unsigned.sh <bundle-id>}"

mkdir -p build
rm -rf build/sts2.xcarchive

xcodebuild archive \
  -project sts2.xcodeproj \
  -scheme sts2 \
  -configuration Release \
  -sdk iphoneos \
  -arch arm64 \
  -archivePath "$PWD/build/sts2.xcarchive" \
  CODE_SIGN_IDENTITY="" \
  CODE_SIGNING_REQUIRED=NO \
  CODE_SIGNING_ALLOWED=NO \
  DEVELOPMENT_TEAM="" \
  PRODUCT_BUNDLE_IDENTIFIER="$BUNDLE_ID" \
  -quiet 2>&1 | tee build/archive.log
