#!/usr/bin/python

import urllib2
import re
from PIL import Image
import StringIO

u = urllib2.urlopen('http://www.reddit.com/r/fffffffuuuuuuuuuuuu/stylesheet.css')
css = u.read()
u.close()

images = {}
icons = {}

for match in re.findall('(a\[href="/[^"]+"\]:after[^{]+){([^}]+)}', css, re.DOTALL):
	names = re.findall('a\[href="/([^"]+)"\]:after', match[0])
	
	for name in names:
		if name not in icons:
			icons[name] = {'url': None, 'width': 25, 'height': 25, 'left': 0, 'top': 0, 'img': None}
	
	ure = re.findall('url\(([^)]+)\)', match[1])
	if ure:
		for name in names:
			icons[name]['url'] = ure[0]
	
	wre = re.findall('width:\\s*(\\d+)px', match[1])
	if wre:
		for name in names:
			icons[name]['width'] = int(wre[0])
	
	hre = re.findall('height:\\s*(\\d+)px', match[1])
	if hre:
		for name in names:
			icons[name]['height'] = int(hre[0])
	
	bpre = re.findall('background-position:\\s*(-?\\d+)(?:px)?\\s+(-?\\d+)(?:px)?', match[1])
	if bpre:
		for name in names:
			icons[name]['left'] = -int(bpre[0][0])
			icons[name]['top'] = -int(bpre[0][1])

f = open("../gtalkchat/gtalkchat.csproj", "r")
csproj = f.read()
f.close()

idx = csproj.find('  </ItemGroup>\n  <Import')
head = csproj[:idx]
tail = csproj[idx:]

for name in icons:
	icon = icons[name]
	
	if not icon['url']:
		print name,"is dead."
		continue
	
	if icon['url'] not in images:
		u = urllib2.urlopen(icon['url'])
		buf = StringIO.StringIO(u.read())
		u.close()
		images[icon['url']] = Image.open(buf)
	
	img = images[icon['url']]
	copy = img.crop((icon['left'], icon['top'], icon['left'] + icon['width'], icon['top'] + icon['height']))
	copy.load()
	if icon['width'] > 48 or icon['height'] > 48:
		if icon['width'] >= icon['height']:
			copy = copy.resize((48, int(round(48 * icon['height'] / float(icon['width'])))), Image.ANTIALIAS)
		else:
			copy = copy.resize((int(round(48 * icon['width'] / float(icon['height']))), 48), Image.ANTIALIAS)
	
	path = 'icons\\emoticon.rage.%s.png' % name.replace('!', '_')
	copy.save('..\\gtalkchat\\' + path)
	
	if path not in head:
		head += '    <Content Include="%s">\n' % path
		head += '      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>\n'
		head += '    </Content>\n'

f = open("../gtalkchat/gtalkchat.csproj", "w")
f.write(head + tail)
f.close()