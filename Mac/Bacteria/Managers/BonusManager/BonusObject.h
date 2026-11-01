//
//  BonusObject.h
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/30/13.
//
//

#import <Foundation/Foundation.h>
#import "Figure.h"
#import "Agent.h"

@interface BonusObject : Agent
{
    BonusObjectType bonusObjectType;
}
@property (readonly) BonusObjectType bonusObjectType;
//
-(id) initWithType:(BonusObjectType)bonusType andPosition:(CGPoint)position;
//
@end
