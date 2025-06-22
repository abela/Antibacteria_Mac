//
//  PeaceFlag.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/26/13.
//
//

#import "PeaceFlag.h"

@implementation PeaceFlag
-(id) initWithType:(BonusObjectType)bonusType andPosition:(CGPoint)position
{
    if((self = [super initWithType:bonusType andPosition:position]))
    {
        bonusObjectType = bonusType;
        sprite.color = ccc3(0, 255, 0);
        return self;
    }
    return nil;
}
@end
