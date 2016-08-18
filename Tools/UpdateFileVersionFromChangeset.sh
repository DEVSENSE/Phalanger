#!/bin/bash

# determining changeset number (just the last git-tfs-id)
CHANGESET="$(git log --grep "git-tfs-id" --max-count=1 | grep git-tfs-id | egrep -o '[[:digit:]]+$')"
while [ -n "$1" ] ; do
	"$(dirname $0)/VersionReplacer.exe" "$1" "$CHANGESET"
	shift
done