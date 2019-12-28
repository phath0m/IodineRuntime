.PHONY: clean all docs

PREFIX = /usr/local/lib

OUTPUT_DIR = ./bin

IODINE = $(OUTPUT_DIR)/iodine.exe

IODINE_DEPS += ./bin/LibIodine.dll

IODINE_DOCS += __builtins__
IODINE_DOCS += sys
IODINE_DOCS += os
IODINE_DOCS += math
IODINE_DOCS += struct
IODINE_DOCS += inspect
IODINE_DOCS += fsutils
IODINE_DOCS += random
IODINE_DOCS += psutils
IODINE_DOCS += threading

# These are the only CANONICAL modules in ./modules
# Anything else is experimental or outdated

IODINE_MODS += ./modules/argparse.id
IODINE_MODS += ./modules/base64.id
IODINE_MODS += ./modules/cryptoutils.id
IODINE_MODS += ./modules/collections.id
IODINE_MODS += ./modules/dis.id
IODINE_MODS += ./modules/events.id
IODINE_MODS += ./modules/ints.id
IODINE_MODS += ./modules/iterutils.id
IODINE_MODS += ./modules/hashlib.id
IODINE_MODS += ./modules/functools.id
IODINE_MODS += ./modules/json.id
IODINE_MODS += ./modules/_whirlpool.id
IODINE_MODS += ./modules/argparse2.id

all:$(IODINE_MODS)

%.id:iodine-binaries
	mono ./bin/iodine.exe -c $@

iodine-binaries:
	mkdir -p $(OUTPUT_DIR)
	nuget restore
	xbuild ./Iodine.sln /p:Configuration=Release /p:DefineConstants="COMPILE_EXTRAS" /t:Build "/p:Mono=true;BaseConfiguration=Release"
	
define iodine-check
mono ./bin/iodine.exe -c $(1) ; 
endef

docs:
	$(foreach mod, $(IODINE_DOCS), $(call make-doc,$(mod)))
clean:
	rm -rf bin
	rm -rf src/bin
	xbuild ./Iodine.sln /t:Clean
install:
	mkdir -p $(PREFIX)/iodine/modules/net
	mkdir -p $(PREFIX)/iodine/extensions
	cp $(IODINE) $(PREFIX)/iodine/iodine.exe
	cp -f $(IODINE_DEPS) $(PREFIX)/iodine
	cp -f $(IODINE_MODS) $(PREFIX)/iodine/modules
	cp -f ./bin/extensions/* $(PREFIX)/iodine/extensions
	echo "#! /bin/bash" > /bin/iodine
	echo "/usr/bin/mono $(PREFIX)/iodine/iodine.exe \"\$$@\"" >> /bin/iodine
	grep -v "IODINE" /etc/environment | cat > /tmp/environment
	mv /tmp/environment /etc/environment
	echo "IODINE_HOME=\"$(PREFIX)/iodine\"" >> /etc/environment
	echo "IODINE_MODULES=\"$(PREFIX)/iodine/modules\"" >> /etc/environment
	chmod +x /bin/iodine
install-ion:
	tmpdir="`mktemp -d`" ; \
	cd $$tmpdir ; \
	pwd ; \
	git clone https://github.com/IodineLang/Ion ; \
	cd Ion ; \
	make install ; \
	cd .. ; \
	rm -rf $$tmpdir
