//
//  WeaponFactory.mm
//  Game
//
//  Created by Giorgi Abelashvili on 10/19/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "WeaponFactory.h"


@implementation WeaponFactory
//
+(Weapon*) getWeaponWithType:(WeaponType)weaponType
{
	switch (weaponType) {
		case kPistol:
			break;
		case kRifle:
		default:
			break;
	}
	return nil;
}
//
@end
