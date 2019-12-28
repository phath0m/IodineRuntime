#! /bin/bash

mono -V
make all
sudo make install
sudo make install-ion
cd tests
module_path=$(readlink -f ../modules)
echo $module_path
export IODINE_PATH=$module_path
ion run
