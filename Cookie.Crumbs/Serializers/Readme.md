## Serializers

### Nested Serializer

Due to low WebASM maturity with .NET, technology management has proven frustrating. To reduce dependencies on third party technologies that may not support WebASM, or that may provide inconsistent WebASM behaviour, and also... just for fun... This namespace provides some simple and highly mobile serializers, implementing minimal technological reliance outside of basic collections, basic math, and basic data functions.

As the only challenge with serialization of hierarchical entities, is that key-value segmentation must abide the hierarchical segmentation, but this is not necessarily a trivial determination. For example, given nested `Key:Value` mappings, `String.Split` on a delimiter token will obviate entry ownership. Bad.

### Basic Format

The Serializer uses a basic format with the following core rules;

1. ``Dictionary<string, object>`` Provides the backbone for serialization. Keys-Values are mapped KEY\*VALUE.
2. All collections are grouped { }. The first non-witespace character/byte informs the collection type
3. All groups must be defined by Dictionary<string, object>, which necessitates values prefixed by Key\*

Provides IDictable classes which provide serialization access through this portal and a byte serializer. The nested serializer provides encoding of bytes and streams through a custom base128 encoder, but for more complex structures the binary serializer should be used instead.

Example output from serializing a randomly made dictionary;

```
mHealthy Cookies*
{d
	0~Oatmeal Raisin;
	1~Oatmeal Choc-Chip;
	2~Fruitcake Cookie;
};
Peanut*sSnickerdoodle;
Indulgent*
{m
	Level 1*Choc-Chip;
	Level 2*sDouble Choc;
	Others*
	{m
		White*sChocolate;
		Alternative*sCaramilk;
		Butterscotch*sMmmm;
	};
};
Serving Options*
{l
	milk;
	cookies;
	ice-cream;
};
```
