//
//  DestructionListener.h
//  Shooter
//
//  Created by Giorgi Abelashvili on 11/2/10.
//  Copyright (c) 2010 __MyCompanyName__. All rights reserved.
//

#import "Box2D.h"

class DestructionListener : public b2DestructionListener
{
public:
    
    DestructionListener();
    
    ~DestructionListener();
    
    void SayGoodbye(b2Fixture* fixture);
    void SayGoodbye(b2Joint* joint);
};