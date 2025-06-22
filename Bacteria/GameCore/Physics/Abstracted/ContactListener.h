//
//  ContactListener.h
//  Shooter
//
//  Created by Giorgi Abelashvili on 11/2/10.
//  Copyright (c) 2010 __MyCompanyName__. All rights reserved.
//


#import "Box2D.h"

class ContactListener : public b2ContactListener
{
public:
    
    ContactListener();
    ~ContactListener();
    
    
    void BeginContact(b2Contact* contact);
    void EndContact(b2Contact* contact);
    void PreSolve(b2Contact* contact, const b2Manifold* oldManifold);
    
    void PostSolve(const b2Contact* contact, const b2ContactImpulse* impulse);
    
};