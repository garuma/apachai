#!/bin/zsh

if test $# -lt 2
then
	echo "Usage: ./buildhtml.sh directory-with-tpl-files skeleton.html"
	exit 1
fi

LIST=(`echo $1/*.html.tpl`)
SKELETON=`cat $2`

REPLACE_LIST=(`grep -E '{{(\w+)}}' $2`)

for tpl in $LIST
do
	filename=`echo $tpl | rev | cut -c 5- | rev`
	cp $2 $filename
	for replace in $REPLACE_LIST
	do
		pcregrep -M -o1 "$replace\n((.|\n)+)\n$replace" $tpl > .tmp
		sed -i -e "/$replace/r .tmp" -e "/$replace/d" $filename
		#rm .tmp
	done
done