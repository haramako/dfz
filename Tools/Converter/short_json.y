class ShortJson::Parser

rule

program: value_list

value_list: value ',' value_list { result = val[2].unshift(val[0]) }
          | value                      { result = [val[0]] }

value: literal '(' args ')' { result = {type: val[0]}; make_args(result, val[2]) }
     | literal
     | '[' value_list ']'

args:                { result = [] }
    | key_value_list

/* key_value_list_p: key_value_list | { [] } */

key_value_list: key_value ',' key_value_list { result = [val[0]]+val[2] }
              | key_value                          { result = [val[0]] }

key_value: ident ':' value { result = [val[0], val[2]] }
         | value           { result = [nil, val[0]] }

/* value_list_p: value_list | { [] } */

/* value_list: value ',' value_list { result = [val[0]]+val[2] }
   | value { result = [val[0]] } */

ident: STRING

literal: NUMBER
       | STRING
