#!make
include .env
export

default:	| setup build

init:
	cp .env .env.dist

setup:
	npm install

build:
	npm run build

watch:
	npm run watch

serve:
	./node_modules/.bin/coveops serve \
		--org-id $(COVEO_ORG_ID) \
		--token $(COVEO_TOKEN) \
		--port $(SERVER_PORT)

pack: 
	npm pack

publish:
	npm publish --access public

	
.PHONY: default init setup build serve pack publish