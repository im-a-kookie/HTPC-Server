## Serializers

### Nested Serializer

The Nested Serializer is one of the only things in here. Due to WebASM and platform restrictions, some features don't work super realiably in browser.

As the only challenge with serialization is handling nested collections reliably, I decided to write my own using as few tools as possible, beyond simple arrays, strings, etc.

### Basic Format

The Serializer uses a basic format with the following core rules;

1. ``Dictionary<string, object>`` Provides the backbone for serialization. Keys-Values are mapped KEY\*VALUE.
2. All collections are grouped { }. The first non-witespace character/byte informs the collection type
3. All groups must be defined by Dictionary<string, object>, which necessitates values prefixed by Key\*

Example output from serializing a randomly made dictionary;

```
mHealthy Cookies*
{d
	0~Oatmeal Raisin;
	1~Oatmeal Choc-Chip;
	2~Fruitcake Cookie;
};
Peanut*sSnickerdoodle;
Indulgents*
{m
	Level 1*Choc-Chip;
	Level 2*sDouble Choc;
	Others*
	{m
		White*Chocolate;
		Alternative*Caramilk;
		Butterscotch*Mmmm;
	};
};
Serving Options*
{l
	milk;
	cookies;
	ice-cream;
};
```