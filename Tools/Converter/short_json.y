class ShortJson::Parser

rule

program: value

value: literal '(' args ')' { result = {type: val[0]}; make_args(result, val[2]) }
     | literal
     | '[' value_list ']'

value_or_kv: value { result = val[0] }
           | symbol ':' value { result = [val[0], val[2]] }


args: { result = [[], []] }
	| value_list { result = [val[0], []] }
    | key_value_list { result = [[], val[0]] }
    | value_list ',' key_value_list { result = [val[0], val[2]] }

key_value_list_p: key_value_list | { [] }

key_value_list: key_value ',' key_value_list { result = [val[0]]+val[2] }
          | key_value { result = [val[0]] }

key_value: symbol ':' literal { result = [val[0], val[2]] }

value_list_p: value_list | { [] }

value_list: value ',' value_list { result = [val[0]]+val[2] }
          | value { result = [val[0]] }

symbol: STRING

literal: NUMBER
       | STRING
