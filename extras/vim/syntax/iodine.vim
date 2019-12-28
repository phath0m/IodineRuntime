if exists("b:current_syntax")
  finish
endif

filetype plugin indent on
" show existing tab with 4 spaces width
set tabstop=4
" when indenting with '>', use 4 spaces width
set shiftwidth=4
" On pressing tab, insert 4 spaces
set expandtab

syn match iodineComment "#.*$"
syn region iodineComment start="/\*" end="\*/"
syn match iodineEscape	contained +\\["\\'0abfnrtvx]+

syn match iodineNumber '\d\+'  
syn match iodineNumber '[-+]\d\+' 

syn match iodineNumber '\d\+\.\d*' 
syn match iodineNumber '[-+]\d\+\.\d*'

syn region iodineString start='"' end='"' contains=iodineEscape
syn region iodineString start="'" end="'" contains=iodineEscape
syn keyword iodineKeyword use from if else for while do break self return foreach in as is isnot try except raise with super yield given when default match case var extend extends implements
syn keyword iodineConstants true false null
syn keyword iodineFunctions print println input map filter len map reduce range repr sum typeof typecast open zip
syn keyword iodineTypes func class contract trait mixin enum lambda Bool Float HashMap Int List Object Str Tuple

syn region iodineBlock start="{" end="}" fold transparent contains=ALL


let b:current_syntax = "iodine"

hi def link iodineComment     Comment
hi def link iodinePreproc     Preproc
hi def link iodineKeyword     Statement
hi def link iodineFunctions   Function
hi def link iodineTypes       Type
hi def link iodineString      Constant
hi def link iodineNumber      Constant
hi def link iodineConstants   Constant
